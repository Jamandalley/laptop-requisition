using LaptopRequisition.Application.DTOs;
using LaptopRequisition.Application.DTOs.Notification;
using LaptopRequisition.Application.DTOs.SSO;
using LaptopRequisition.Application.Helpers;
using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Application.Interfaces.External;
using LaptopRequisition.Application.Interfaces.SSO;
using LaptopRequisition.Domain;
using LaptopRequisition.Domain.Enums;
using LaptopRequisition.Application.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Refit;
using LaptopRequisition.Application.DTOs.Login;
using System.IdentityModel.Tokens.Jwt; // Added for JwtSecurityTokenHandler

namespace LaptopRequisition.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ISsoClient _ssoClient;
        private readonly IAdminSsoClient _adminSsoClient; // NEW: Injected IAdminSsoClient
        private readonly SsoSettings _ssoSettings;
        private readonly IOtpHelperService _otpHelperService;
        private readonly INotificationApi _notificationApi;
        private readonly NotificationApiSettings _notificationApiSettings;
        private readonly IRoleRepository _roleRepository;
        private readonly AuthSettings _authSettings;

        public AuthService(IEmployeeRepository employeeRepository,
                           IPasswordResetTokenRepository passwordResetTokenRepository,
                           IDepartmentRepository departmentRepository,
                           ISsoClient ssoClient,
                           IAdminSsoClient adminSsoClient, // NEW: Added to constructor
                           IOptions<SsoSettings> ssoSettingsOptions,
                           IOtpHelperService otpHelperService,
                           INotificationApi notificationApi,
                           IOptions<NotificationApiSettings> notificationApiSettingsOptions,
                           IRoleRepository roleRepository,
                           IOptions<AuthSettings> authSettingsOptions)
        {
            _employeeRepository = employeeRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _departmentRepository = departmentRepository;
            _ssoClient = ssoClient;
            _adminSsoClient = adminSsoClient; // NEW: Initialized
            _ssoSettings = ssoSettingsOptions.Value;
            _otpHelperService = otpHelperService;
            _notificationApi = notificationApi;
            _notificationApiSettings = notificationApiSettingsOptions.Value;
            _roleRepository = roleRepository;
            _authSettings = authSettingsOptions.Value;
        }

        public async Task<Employee> RegisterEmployeeAsync(RegisterEmployeeDto registerDto)
        {
            var existingEmployeeByStaffId = await _employeeRepository.GetByStaffIdAsync(registerDto.StaffId);
            if (existingEmployeeByStaffId != null)
            {
                throw new InvalidOperationException("Staff ID is already registered.");
            }

            var existingEmployeeByEmail = await _employeeRepository.GetByEmailAsync(registerDto.Email);
            if (existingEmployeeByEmail != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }
            
            var department = await _departmentRepository.GetByIdAsync(registerDto.DepartmentId); 
            if (department == null)
            {
                throw new InvalidOperationException($"Department with ID '{registerDto.DepartmentId}' not found."); 
            }

            // Automatically assign "Employee" role
            var employeeRole = await _roleRepository.GetByNameAsync("Employee");
            if (employeeRole == null)
            {
                throw new InvalidOperationException("Default 'Employee' role not found. Please ensure it is seeded in the database.");
            }
         
            // --- NEW: OTP Validity Check ---
            var otpValidityResult = await _otpHelperService.CheckOtpValidityAsync(registerDto.ValidationReference, registerDto.Email);
            if (!otpValidityResult.IsSuccessful)
            {
                throw new InvalidOperationException(otpValidityResult.Message ?? "OTP validation failed during registration.");
            }
            // --- END NEW ---

            var newEmployeeId = Guid.NewGuid();
            
            var ssoUserRequest = new SsoUserCreationRequestDto
            {
                Username = newEmployeeId.ToString(), 
                Password = registerDto.Password,
                Email = registerDto.Email,
                SourceId = newEmployeeId.ToString() 
            };

            try
            {
                var ssoResponse = await _ssoClient.CreateSsoUser(_ssoSettings.ClientId, ssoUserRequest);
                if (!ssoResponse.IsSuccess)
                {
                    throw new InvalidOperationException($"SSO user creation failed: {ssoResponse.Message ?? "Unknown error"}");
                }
            }
            catch (ApiException ex)
            {
                throw new InvalidOperationException($"Failed to create user in SSO system. Status: {ex.StatusCode}. Message: {ex.Content}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An unexpected error occurred during SSO user creation: {ex.Message}", ex);
            }
            
            var employee = new Employee
            {
                Id = newEmployeeId, 
                StaffId = registerDto.StaffId,
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                DepartmentId = registerDto.DepartmentId, 
                RoleId = employeeRole.Id, // Assign the ID of the "Employee" role
                PasswordHash = string.Empty,
                FailedLoginCount = 0,
                IsLocked = false,
                PreviousPasswordHashes = JsonSerializer.Serialize(new List<string>()),
                IsVerified = true // NEW: OTP already verified at this point
            };

            await _employeeRepository.AddAsync(employee);
            
            return employee;
        }

        public async Task<LoginResponseDto> LoginAsync(string email, string password)
        {
            var response = new LoginResponseDto { IsSuccess = false };
            Employee? employee = null; 

            SsoTokenResponseDto ssoTokenResponse;
            try
            {
                // --- SSO Authentication ---
                // Determine SSO username based on whether local employee exists
                string ssoUsername;
                var existingLocalEmployee = await _employeeRepository.GetByEmailWithDepartmentAndRoleAsync(email);

                if (existingLocalEmployee != null)
                {
                    ssoUsername = existingLocalEmployee.Id.ToString(); // Use local employee's GUID as SSO username
                }
                else
                {
                    // For SSO-only users (like super admin), use email as username for SSO
                    ssoUsername = email; 
                }

                var ssoTokenRequest = new SsoTokenRequestDto
                {
                    ClientId = _ssoSettings.ClientId,
                    ClientSecret = _ssoSettings.ClientSecret,
                    Username = ssoUsername, 
                    Password = password
                };

                // --- NEW DEBUG LOGGING ---
                Console.WriteLine($"[SSO Login Debug] Attempting SSO login for email: {email}");
                Console.WriteLine($"[SSO Login Debug] SSO ClientId: {_ssoSettings.ClientId}");
                Console.WriteLine($"[SSO Login Debug] SSO Username sent: {ssoTokenRequest.Username}");
                // Resolve ambiguity for interpolated string
                var passwordSnippet = ssoTokenRequest.Password?.Substring(0, Math.Min(ssoTokenRequest.Password.Length, 3));
                Console.WriteLine($"[SSO Login Debug] SSO Password sent (first 3 chars): {passwordSnippet}...");
                Console.WriteLine($"[SSO Login Debug] SSO GrantType sent: {ssoTokenRequest.GrantType}");
                // --- END NEW DEBUG LOGGING ---

                try
                {
                    ssoTokenResponse = await _ssoClient.GetSsoToken(ssoTokenRequest);

                    Console.WriteLine("[SSO Login Debug] TOKEN RECEIVED SUCCESSFULLY");
                    var accessTokenSnippet = ssoTokenResponse?.AccessToken?.Substring(0, Math.Min(ssoTokenResponse.AccessToken.Length, 50));
                    Console.WriteLine($"[SSO Login Debug] AccessToken: {accessTokenSnippet}...");
                    Console.WriteLine($"[SSO Login Debug] TokenType: {ssoTokenResponse?.TokenType}");
                    Console.WriteLine($"[SSO Login Debug] ExpiresIn: {ssoTokenResponse?.ExpiresIn}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SSO Login Debug] TOKEN ERROR DURING SSO CLIENT CALL:");
                    Console.WriteLine(ex.ToString());

                    // Re-throw to be caught by the outer ApiException/Exception block
                    throw; 
                }

                // Map SsoTokenResponseDto to SsoTokenDetailsDto for LoginResponseDto
                response.TokenDetails = new SsoTokenDetailsDto
                {
                    AccessToken = ssoTokenResponse.AccessToken,
                    ExpiresIn = ssoTokenResponse.ExpiresIn,
                    TokenType = ssoTokenResponse.TokenType,
                    Scope = ssoTokenResponse.Scope ?? string.Empty 
                };
                response.IsSuccess = true;
                response.Message = "Login successful.";

                // --- Extract claims from SSO token ---
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(ssoTokenResponse.AccessToken);
                
                var ssoUserEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? email;
                // Resolve ambiguity for Guid.TryParse
                var sourceIdClaimValue = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == "SourceId")
                    ?.Value;
                var ssoFullName = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                
                if (sourceIdClaimValue == null || !Guid.TryParse(sourceIdClaimValue, out Guid employeeId))
                {
                    throw new InvalidOperationException("SSO token does not contain a valid user ID.");
                }

                // Extract roles from SSO token (Keycloak specific claims)
                var ssoRoles = new List<string>();
                var realmAccessClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "realm_access");
                if (realmAccessClaim != null)
                {
                    using (JsonDocument doc = JsonDocument.Parse(realmAccessClaim.Value))
                    {
                        if (doc.RootElement.TryGetProperty("roles", out JsonElement rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                        {
                            ssoRoles.AddRange(rolesElement.EnumerateArray().Select(r => r.GetString() ?? string.Empty));
                        }
                    }
                }
                var resourceAccessClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "resource_access");
                if (resourceAccessClaim != null)
                {
                    using (JsonDocument doc = JsonDocument.Parse(resourceAccessClaim.Value))
                    {
                        foreach (var clientProperty in doc.RootElement.EnumerateObject())
                        {
                            if (clientProperty.Value.TryGetProperty("roles", out JsonElement rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                            {
                                ssoRoles.AddRange(rolesElement.EnumerateArray().Select(r => r.GetString() ?? string.Empty));
                            }
                        }
                    }
                }
                
                // --- Local Employee Handling ---
                // Try to find local employee by SSO ID (which is now the Employee.Id)
                employee = await _employeeRepository.GetByIdWithDepartmentAndRoleAsync(employeeId); 

                if (employee != null)
                {
                    // Existing local employee: apply local checks
                    response.EmployeeDetails = MapEmployeeToDto(employee);
                    response.IsFirstLogin = employee.IsFirstLogin;
                    response.IsAdmin = (employee.Role?.Name == "Admin"); // Use local role

                    if (employee.IsLocked)
                    {
                        if (employee.LockoutEndDate.HasValue && employee.LockoutEndDate > DateTime.UtcNow)
                        {
                            response.Message = $"Account is locked. Try again after {employee.LockoutEndDate.Value.ToLocalTime()}.";
                            response.IsLocked = true;
                            response.LockoutEndDate = employee.LockoutEndDate;
                            response.IsSuccess = false; // Local lockout overrides SSO success
                            return response;
                        }
                        else
                        {
                            // Lockout period expired, reset lockout
                            employee.IsLocked = false;
                            employee.FailedLoginCount = 0;
                            employee.LockoutEndDate = null;
                            await _employeeRepository.UpdateLoginAttemptsAsync(employee);
                        }
                    }

                    if (!employee.IsVerified)
                    {
                        response.Message = "Account not verified. Please verify your account first.";
                        response.IsSuccess = false; // Local verification overrides SSO success
                        return response;
                    }

                    // Reset failed login attempts on successful login
                    if (employee.FailedLoginCount > 0 || employee.IsLocked)
                    {
                        employee.FailedLoginCount = 0;
                        employee.IsLocked = false;
                        employee.LockoutEndDate = null;
                        await _employeeRepository.UpdateLoginAttemptsAsync(employee);
                    }
                }
                else
                {
                    // SSO-only user (e.g. Super Admin)
                    // Resolve ambiguity for Guid.TryParse
                    var actualSsoUserIdString = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value as string;

                    response.EmployeeDetails = new EmployeeDto
                    {
                        Id = actualSsoUserIdString != null && Guid.TryParse(actualSsoUserIdString, out var adminId)
                            ? adminId
                            : Guid.Empty,

                        Email = ssoUserEmail,
                        FullName = ssoFullName ?? ssoUserEmail,

                        DepartmentName = "SSO Managed",

                        StaffId = actualSsoUserIdString != null
                            ? "SSO-" + actualSsoUserIdString.Substring(0, 8)
                            : "SSO-ADMIN",

                        Role = ssoRoles.Contains("admin")
                            ? "Admin"
                            : "Employee",

                        IsLocked = false,
                        IsFirstLogin = false
                    };

                    response.IsAdmin = ssoRoles.Contains("admin");
                    response.IsFirstLogin = false;
                }
            }
            catch (ApiException ex)
            {
                Console.WriteLine($"[SSO Login Debug] ApiException caught in outer block. Status: {ex.StatusCode}, Content: {ex.Content}"); // Added
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    employee = await _employeeRepository.GetByEmailWithDepartmentAndRoleAsync(email);

                    if (employee != null) 
                    {
                        employee.FailedLoginCount++;
                        if (employee.FailedLoginCount >= _authSettings.MaxFailedLoginAttempts)
                        {
                            employee.IsLocked = true;
                            employee.LockoutEndDate = DateTime.UtcNow.AddMinutes(_authSettings.LockoutDurationMinutes);
                            response.IsLocked = true;
                            response.LockoutEndDate = employee.LockoutEndDate;
                            response.Message = $"Account locked due to too many failed attempts. Try again after {employee.LockoutEndDate.Value.ToLocalTime()}.";
                        }
                        else
                        {
                            response.Message = "Invalid credentials.";
                        }
                        await _employeeRepository.UpdateLoginAttemptsAsync(employee);
                    }
                    else
                    {
                        // SSO-only user failed login - no local tracking, just return invalid credentials
                        response.Message = "Invalid credentials.";
                    }
                }
                else
                {
                    response.Message = $"Failed to authenticate with SSO system. Status: {ex.StatusCode}. Message: {ex.Content}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SSO Login Debug] Unexpected Exception in outer block: {ex.Message}"); // Added
                response.Message = $"An unexpected error occurred during SSO authentication: {ex.Message}";
            }
            
            return response;
        }

        public async Task<LoginResponseDto> AdminLoginAsync(string email, string password)
        {
            var loginResponse = new LoginResponseDto { IsSuccess = false };
            SsoLoginResponseRootDto ssoLoginRootResponse; // Declare here to be accessible outside try block

            try
            {
                var ssoLoginRequest = new SsoLoginRequestDto
                {
                    Username = email,
                    Password = password
                };

                // Changed to use _adminSsoClient
                ssoLoginRootResponse = await _adminSsoClient.LoginSsoUser(ssoLoginRequest);
            }
            catch (ApiException ex)
            {
                Console.WriteLine($"[SSO Admin Login Debug] ApiException caught. Status: {ex.StatusCode}, Content: {ex.Content}");

                // NEW: Handle ApiException with OK status code (Refit deserialization issue)
                if (ex.StatusCode == System.Net.HttpStatusCode.OK && !string.IsNullOrEmpty(ex.Content))
                {
                    try
                    {
                        // Attempt manual deserialization
                        ssoLoginRootResponse = JsonSerializer.Deserialize<SsoLoginResponseRootDto>(ex.Content);
                        // If successful, we can proceed with the rest of the logic outside this catch block
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"[SSO Admin Login Debug] Manual JSON deserialization failed: {jsonEx.Message}");
                        loginResponse.Message = $"Failed to process SSO response: {jsonEx.Message}";
                        return loginResponse; // Return failure if manual deserialization fails
                    }
                }
                else
                {
                    // For other API exceptions (non-OK status codes), treat as a failure
                    loginResponse.Message = $"Failed to authenticate with SSO system for Admin. Status: {ex.StatusCode}. Message: {ex.Content}";
                    return loginResponse;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SSO Admin Login Debug] Unexpected Exception: {ex.Message}");
                loginResponse.Message = $"An unexpected error occurred during SSO Admin authentication: {ex.Message}";
                return loginResponse;
            }

            // Original logic to process ssoLoginRootResponse (now guaranteed to be populated if no hard errors)
            if (!ssoLoginRootResponse.IsSuccessful || ssoLoginRootResponse.Data == null)
            {
                loginResponse.Message = ssoLoginRootResponse.Message ?? "SSO Admin login failed.";
                return loginResponse;
            }

            var ssoTokenDetails = ssoLoginRootResponse.Data.TokenDetails;
            var ssoProfile = ssoLoginRootResponse.Data.Profile;

            loginResponse.IsSuccess = true;
            loginResponse.Message = ssoLoginRootResponse.Message;
            loginResponse.TokenDetails = ssoTokenDetails;

            // Map SSO Profile to EmployeeDto
            loginResponse.EmployeeDetails = new EmployeeDto
            {
                // Resolve ambiguity for Guid.TryParse
                Id = Guid.TryParse(ssoProfile.Id as string, out var profileId) ? profileId : Guid.Empty,
                StaffId = ssoProfile.Id, // Using SSO ID as StaffId for SSO-managed users
                FullName = $"{ssoProfile.FirstName} {ssoProfile.LastName}",
                Email = ssoProfile.Email,
                PhoneNumber = ssoProfile.PhoneNumber,
                // FIX: Use Guid.TryParse for DepartmentId
                DepartmentId = Guid.TryParse(ssoProfile.DepartmentId, out Guid parsedDepartmentId) ? parsedDepartmentId : Guid.Empty,
                DepartmentName = "SSO Managed", // Or fetch from local DB if departmentId maps
                Role = ssoProfile.Roles.Contains("REQUISITION_PORTAL_ADMIN") || ssoProfile.Roles.Contains("Super Admin") ? "Admin" : "Employee", // Determine role based on SSO roles
                IsLocked = ssoProfile.Status != "Active",
                IsFirstLogin = false // Assuming SSO admin users are not first-time logins
            };

            // Check for Admin role based on the provided sample roles
            loginResponse.IsAdmin = ssoProfile.Roles.Contains("REQUISITION_PORTAL_ADMIN") || ssoProfile.Roles.Contains("Super Admin");

            if (!loginResponse.IsAdmin)
            {
                loginResponse.IsSuccess = false;
                loginResponse.Message = "Access denied. Only administrators can log in to the Admin Portal.";
                loginResponse.TokenDetails = new SsoTokenDetailsDto(); // Clear token if not admin
                loginResponse.EmployeeDetails = new EmployeeDto(); // Clear employee details
            }
            
            return loginResponse;
        }

        public async Task VerifyAccountAsync(string validationReference, string otp)
        {
            var otpValidationResult = await _otpHelperService.ValidateOtpAsync(validationReference, otp);

            // --- FIX: Use the message from otpValidationResult if it's not successful ---
            if (!otpValidationResult.IsSuccessful)
            {
                throw new InvalidOperationException(
                    otpValidationResult.Message ?? "OTP verification failed.");
            }
            // --- END FIX ---
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var employee = await _employeeRepository.GetByEmailWithDepartmentAndRoleAsync(email);
            if (employee == null)
            {
                return true; // Security by obscurity
            }

            if (employee.PasswordHash == string.Empty) // SSO managed user
            {
                throw new InvalidOperationException("Password reset for this account is managed by the SSO system. Please use the SSO portal's password reset functionality.");
            }

            var token = Guid.NewGuid().ToString();
            var passwordResetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            };

            await _passwordResetTokenRepository.AddAsync(passwordResetToken);
            
            var resetLink = $"https://yourdomain.com/reset-password?token={token}";
            // Replaced direct email service with Notification API
            var emailBody = await BuildPasswordResetEmailBodyAsync(resetLink, employee.FullName);
            var notificationRequest = new NotificationRequest
            {
                Channels = new List<string> { "Email" },
                From = _notificationApiSettings.FromEmail,
                To = employee.Email,
                Subject = "Password Reset Request",
                Message = emailBody
            };
            var notificationResponse = await _notificationApi.SendNotificationAsync(notificationRequest);

            if (!notificationResponse.IsSuccessStatusCode || !notificationResponse.Content.IsSuccessful)
            {
                // Log error if notification failed
                throw new InvalidOperationException($"Failed to send password reset email: {notificationResponse.Error?.Content}");
            }

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(token);

            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Invalid or expired password reset token.");
            }

            // Safely access EmployeeId from the nullable property
            if (!resetToken.EmployeeId.HasValue)
            {
                throw new InvalidOperationException("Password reset token is not associated with an employee.");
            }

            var employee = await _employeeRepository.GetByIdWithDepartmentAndRoleAsync(resetToken.EmployeeId.Value);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found for the given token.");
            }

            if (employee.PasswordHash == string.Empty) // SSO managed user
            {
                throw new InvalidOperationException("Password reset for this account is managed by the SSO system. Please use the SSO portal's password reset functionality.");
            }
            
            var previousHashes = JsonSerializer.Deserialize<List<string>>(employee.PreviousPasswordHashes)
                                 ?? new List<string>(); 
            foreach (var oldHash in previousHashes)
            {
                if (BCrypt.Net.BCrypt.Verify(newPassword, oldHash))
                {
                    throw new InvalidOperationException("New password cannot be one of the last 3 used passwords.");
                }
            }

            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            previousHashes.Add(newPasswordHash);
            if (previousHashes.Count > 3)
            {
                previousHashes.RemoveAt(0); 
            }
            employee.PreviousPasswordHashes = JsonSerializer.Serialize(previousHashes);
            
            employee.PasswordHash = newPasswordHash;
            employee.FailedLoginCount = 0;
            employee.IsLocked = false;
            
            resetToken.IsUsed = true;

            await _employeeRepository.UpdateAsync(employee);
            await _passwordResetTokenRepository.UpdateAsync(resetToken);

            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid employeeId, string currentPassword, string newPassword)
        {
            var employee = await _employeeRepository.GetByIdWithDepartmentAndRoleAsync(employeeId);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }
            
            if (employee.PasswordHash == string.Empty) // SSO managed user
            {
                throw new InvalidOperationException("Password change for this account is managed by the SSO system. Please use the SSO portal's password change functionality.");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, employee.PasswordHash))
            {
                throw new UnauthorizedAccessException("Incorrect current password.");
            }
            
            var previousHashes = JsonSerializer.Deserialize<List<string>>(employee.PreviousPasswordHashes)
                                 ?? new List<string>(); 
            foreach (var oldHash in previousHashes)
            {
                if (BCrypt.Net.BCrypt.Verify(newPassword, oldHash))
                {
                    throw new InvalidOperationException("New password cannot be one of the last 3 used passwords.");
                }
            }

            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            previousHashes.Add(newPasswordHash);
            if (previousHashes.Count > 3)
            {
                previousHashes.RemoveAt(0); 
            }
            employee.PreviousPasswordHashes = JsonSerializer.Serialize(previousHashes);
            
            employee.PasswordHash = newPasswordHash;
            employee.FailedLoginCount = 0;
            employee.IsLocked = false;

            await _employeeRepository.UpdateAsync(employee);

            return true;
        }

        private EmployeeDto MapEmployeeToDto(Employee employee)
        {
             return new EmployeeDto
            {
                Id = employee.Id,
                StaffId = employee.StaffId,
                FullName = employee.FullName,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                DepartmentId = employee.DepartmentId,
                DepartmentName = employee.Department?.Name ?? "Unknown", 
                Role = employee.Role?.Name ?? "Unknown", 
                IsLocked = employee.IsLocked,
                IsFirstLogin = employee.IsFirstLogin // Added
            };
        }

        
        private async Task<string> BuildPasswordResetEmailBodyAsync(string resetLink, string employeeName)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "PasswordReset.html");
            if (!File.Exists(templatePath))
            {
                // Fallback to a simple message if template not found
                return $"Dear {employeeName},\n\nYour password reset link is: {resetLink}\n\nBest regards,\nLRS Team";
            }
            var body = await File.ReadAllTextAsync(templatePath);
            return body
                .Replace("{{employeeName}}", employeeName)
                .Replace("{{resetLink}}", resetLink);
        }

        // Helper method to build welcome email body from template
        private async Task<string> BuildWelcomeEmailBodyAsync(string employeeName)
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "Welcome.html");
            if (!File.Exists(templatePath))
            {
                // Fallback to a simple message if template not found
                return $"Dear {employeeName},\n\nYour account has been successfully created. You can now log in to request laptops.\n\nBest regards,\nLRS Team";
            }
            var body = await File.ReadAllTextAsync(templatePath);
            return body
                .Replace("{{employeeName}}", employeeName);
        }
    }
}