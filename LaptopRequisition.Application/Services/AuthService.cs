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
        // Removed: private readonly IAdminSsoClient _adminSsoClient; // Removed
        private readonly SsoSettings _ssoSettings;
        private readonly IOtpHelperService _otpHelperService;
        private readonly INotificationApi _notificationApi;
        private readonly NotificationApiSettings _notificationApiSettings;
        private readonly IRoleRepository _roleRepository;
        private readonly AuthSettings _authSettings;
        private readonly ISsoPasswordResetClient _ssoPasswordResetClient; // NEW: Injected ISsoPasswordResetClient

        public AuthService(IEmployeeRepository employeeRepository,
                           IPasswordResetTokenRepository passwordResetTokenRepository,
                           IDepartmentRepository departmentRepository,
                           ISsoClient ssoClient,
                           // Removed: IAdminSsoClient adminSsoClient, // Removed from constructor
                           IOptions<SsoSettings> ssoSettingsOptions,
                           IOtpHelperService otpHelperService,
                           INotificationApi notificationApi,
                           IOptions<NotificationApiSettings> notificationApiSettingsOptions,
                           IRoleRepository roleRepository,
                           IOptions<AuthSettings> authSettingsOptions,
                           ISsoPasswordResetClient ssoPasswordResetClient) // NEW: Added to constructor
        {
            _employeeRepository = employeeRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _departmentRepository = departmentRepository;
            _ssoClient = ssoClient;
            // Removed: _adminSsoClient = adminSsoClient; // Removed
            _ssoSettings = ssoSettingsOptions.Value;
            _otpHelperService = otpHelperService;
            _notificationApi = notificationApi;
            _notificationApiSettings = notificationApiSettingsOptions.Value;
            _roleRepository = roleRepository;
            _authSettings = authSettingsOptions.Value;
            _ssoPasswordResetClient = ssoPasswordResetClient; // NEW: Initialized
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
                var ssoFullName = jwtToken.Claims.FirstOrDefault(c => c.Type == "Us_FullName")?.Value; // FIX: Use Us_FullName claim
                
                Guid employeeId;
                if (sourceIdClaimValue == null || !Guid.TryParse(sourceIdClaimValue, out employeeId))
                {
                    // If SourceId is not a GUID (e.g., for super admin or other SSO-only users),
                    // try to use 'sub' claim as a fallback for a unique identifier.
                    // This might not be a GUID, so we'll store it as a string in EmployeeDto.Id for SSO-only users.
                    var subClaimValue = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                    if (subClaimValue != null && Guid.TryParse(subClaimValue, out employeeId))
                    {
                        // It's a GUID, use it
                    }
                    else
                    {
                        // Not a GUID, or null. Assign Guid.Empty and handle as SSO-only user without local GUID.
                        employeeId = Guid.Empty;
                    }
                }

                // Extract roles from SSO token (Keycloak specific claims)
                var ssoRoles = new List<string>();
                var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList(); // FIX: Get all role claims
                ssoRoles.AddRange(roleClaims);
                
                // --- Local Employee Handling ---
                // Try to find local employee by SSO ID (which is now the Employee.Id)
                employee = await _employeeRepository.GetByIdWithDepartmentAndRoleAsync(employeeId); 

                if (employee != null)
                {
                    // Existing local employee: apply local checks
                    response.EmployeeDetails = MapEmployeeToDto(employee);
                    response.IsFirstLogin = employee.IsFirstLogin;
                    response.IsAdmin = (employee.Role?.Name == "Admin" || employee.Role?.Name == "Super Admin"); // Use local role

                    // --- NEW: Role Synchronization ---
                    var highestSsoRole = await GetHighestRoleFromSso(ssoRoles);
                    if (highestSsoRole != null && employee.Role?.Id != highestSsoRole.Id) // Compare by ID
                    {
                        employee.RoleId = highestSsoRole.Id;
                        employee.Role = highestSsoRole; // Update navigation property for immediate use
                        await _employeeRepository.UpdateAsync(employee);
                        response.EmployeeDetails.Role = highestSsoRole.Name; // Update DTO
                        response.IsAdmin = (highestSsoRole.Name == "Admin" || highestSsoRole.Name == "REQUISITION_PORTAL_ADMIN" || highestSsoRole.Name == "Super Admin"); // Update IsAdmin based on new role
                    }
                    // --- END NEW ---

                    if (employee.IsLocked)
                    {
                        if (employee.LockoutEndDate.HasValue)
                        {
                            if (employee.LockoutEndDate > DateTime.UtcNow)
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
                        else
                        {
                            // Administrative deactivation (IsLocked is true, but LockoutEndDate is null)
                            response.Message = "Your account has been deactivated. Please contact the administrator.";
                            response.IsSuccess = false;
                            return response;
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
                    // SSO-only user (e.g. Super Admin or other roles not locally managed)
                    response.EmployeeDetails = new EmployeeDto
                    {
                        Id = employeeId, // Use the parsed GUID or Guid.Empty
                        Email = ssoUserEmail,
                        FullName = ssoFullName ?? ssoUserEmail,
                        DepartmentName = "SSO Managed",
                        StaffId = ssoUsername, // Use the SSO username (GUID or email)
                        Role = ssoRoles.Contains("REQUISITION_PORTAL_ADMIN") || ssoRoles.Contains("Super Admin") ? "Admin" : "Employee", // Determine role based on SSO roles
                        IsLocked = false,
                        IsFirstLogin = false
                    };

                    response.IsAdmin = ssoRoles.Contains("Admin") || ssoRoles.Contains("REQUISITION_PORTAL_ADMIN") || ssoRoles.Contains("Super Admin");
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

        public async Task VerifyAccountAsync(string validationReference, string stringOtp) // FIX: Renamed otp to stringOtp
        {
            var otpValidationResult = await _otpHelperService.ValidateOtpAsync(validationReference, stringOtp); // FIX: Use stringOtp

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
            if (string.IsNullOrWhiteSpace(email))
            {
                return true; // Security by obscurity
            }

            var employee = await _employeeRepository.GetByEmailWithDepartmentAndRoleAsync(email);
            if (employee == null)
            {
                return true; // Security by obscurity
            }

            // --- ALWAYS Call SSO for password reset initiation ---
            try
            {
                var ssoRequest = new SsoInitiatePasswordResetRequestDto
                {
                    Username = employee.Id.ToString() // Use employee's GUID as username for SSO password reset
                };
                var ssoResponse = await _ssoPasswordResetClient.InitiatePasswordReset(ssoRequest);

                if (ssoResponse.Content == null || !ssoResponse.Content.IsSuccess)
                {
                    throw new InvalidOperationException(ssoResponse.Content?.Message ?? "SSO password reset initiation failed.");
                }

                var ssoToken = ssoResponse.Content.Data?.PasswordResetToken;
                if (string.IsNullOrEmpty(ssoToken))
                {
                    throw new InvalidOperationException("SSO password reset initiation succeeded but returned no token.");
                }

                var ssoPasswordResetToken = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    Token = ssoToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                    IsUsed = false
                };

                await _passwordResetTokenRepository.AddAsync(ssoPasswordResetToken);

                var ssoResetLink = $"{_authSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(ssoToken)}&employeeId={employee.Id}";
                var ssoEmailBody = await BuildPasswordResetEmailBodyAsync(ssoResetLink, employee.FullName);
                var ssoNotificationRequest = new NotificationRequest
                {
                    Channels = new List<string> { "Email" },
                    From = _notificationApiSettings.FromEmail,
                    To = employee.Email,
                    Subject = "Password Reset Request",
                    Message = ssoEmailBody
                };
                var ssoNotificationResponse = await _notificationApi.SendNotificationAsync(ssoNotificationRequest);

                if (!ssoNotificationResponse.IsSuccessStatusCode || !ssoNotificationResponse.Content.IsSuccessful)
                {
                    throw new InvalidOperationException($"Failed to send password reset email: {ssoNotificationResponse.Error?.Content}");
                }

                return true;
            }
            catch (ApiException ex)
            {
                throw new InvalidOperationException($"Failed to initiate password reset with SSO system. Status: {ex.StatusCode}. Message: {ex.Content}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An unexpected error occurred during SSO password reset initiation: {ex.Message}", ex);
            }
        }

        public async Task<bool> ResetPasswordAsync(string employeeId, string token, string newPassword)
        {
            if (!Guid.TryParse(employeeId, out var employeeGuid))
            {
                throw new InvalidOperationException("Invalid employee ID format.");
            }

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

            if (resetToken.EmployeeId.Value != employeeGuid)
            {
                throw new InvalidOperationException("Password reset token does not belong to the specified employee.");
            }

            var employee = await _employeeRepository.GetByIdWithDepartmentAndRoleAsync(resetToken.EmployeeId.Value);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found for the given token.");
            }

            var previousHashes = new List<string>();
            if (employee.PasswordHash != string.Empty)
            {
                previousHashes = JsonSerializer.Deserialize<List<string>>(employee.PreviousPasswordHashes)
                                 ?? new List<string>(); 
                foreach (var oldHash in previousHashes)
                {
                    if (BCrypt.Net.BCrypt.Verify(newPassword, oldHash))
                    {
                        throw new InvalidOperationException("New password cannot be one of the last 3 used passwords.");
                    }
                }
            }

            // --- ALWAYS Call SSO for password reset completion ---
            try
            {
                var ssoRequest = new SsoCompletePasswordResetRequestDto
                {
                    Username = employee.Id.ToString(), // Use employee's GUID as username
                    PasswordResetToken = token,
                    NewPassword = newPassword
                };
                var ssoResponse = await _ssoPasswordResetClient.CompletePasswordReset(ssoRequest);

                if (!ssoResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"SSO password reset completion failed. Status: {ssoResponse.StatusCode}.");
                }
                if (ssoResponse.Content == null || !ssoResponse.Content.IsSuccess)
                {
                    throw new InvalidOperationException(ssoResponse.Content?.Message ?? "SSO password reset completion failed.");
                }
                
                // If SSO reset is successful, mark local token as used
                resetToken.IsUsed = true;
                await _passwordResetTokenRepository.UpdateAsync(resetToken);
            }
            catch (ApiException ex)
            {
                throw new InvalidOperationException($"Failed to complete password reset with SSO system. Status: {ex.StatusCode}. Message: {ex.Content}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An unexpected error occurred during SSO password reset completion: {ex.Message}", ex);
            }
            
            if (employee.PasswordHash != string.Empty)
            {
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
            }

            return true;
        }

        public async Task<bool> ChangePasswordAsync(string employeeId, string currentPassword, string newPassword)
        {
            if (!Guid.TryParse(employeeId, out var employeeGuid))
            {
                throw new InvalidOperationException("Invalid employee ID format.");
            }

            var employee = await _employeeRepository.GetByIdWithDepartmentAndRoleAsync(employeeGuid);
            if (employee == null)
            {
                throw new InvalidOperationException("Employee not found.");
            }
            
            var previousHashes = new List<string>();
            if (employee.PasswordHash != string.Empty)
            {
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, employee.PasswordHash))
                {
                    throw new UnauthorizedAccessException("Incorrect current password.");
                }
                
                previousHashes = JsonSerializer.Deserialize<List<string>>(employee.PreviousPasswordHashes)
                                     ?? new List<string>(); 
                foreach (var oldHash in previousHashes)
                {
                    if (BCrypt.Net.BCrypt.Verify(newPassword, oldHash))
                    {
                        throw new InvalidOperationException("New password cannot be one of the last 3 used passwords.");
                    }
                }
            }

            // ALWAYS call SSO to change password
            try
            {
                var ssoRequest = new SsoChangePasswordRequestDto
                {
                    Username = employee.Id.ToString(),
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                };
                var ssoResponse = await _ssoPasswordResetClient.ChangePassword(ssoRequest);

                if (!ssoResponse.IsSuccessStatusCode)
                {
                    var errorMsg = ssoResponse.Error?.Content ?? "No extra details.";
                    throw new InvalidOperationException($"SSO password change failed. Status: {ssoResponse.StatusCode}. Details: {errorMsg}");
                }
                if (ssoResponse.Content == null || !ssoResponse.Content.IsSuccess)
                {
                    throw new InvalidOperationException(ssoResponse.Content?.Message ?? "SSO password change failed.");
                }
            }
            catch (ApiException ex)
            {
                throw new InvalidOperationException($"Failed to change password with SSO system. Status: {ex.StatusCode}. Message: {ex.Content}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An unexpected error occurred during SSO password change: {ex.Message}", ex);
            }

            if (employee.PasswordHash != string.Empty)
            {
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
            }

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

        // NEW: Helper method to get the highest role from SSO claims that matches a local role
        private async Task<Role?> GetHighestRoleFromSso(List<string> ssoRoles)
        {
            var allLocalRoles = await _roleRepository.GetAllAsync(); // Assuming this method exists and returns all roles

            // Prioritize specific admin roles
            if (ssoRoles.Contains("Super Admin"))
            {
                return allLocalRoles.FirstOrDefault(r => r.Name == "Super Admin");
            }
            if (ssoRoles.Contains("REQUISITION_PORTAL_ADMIN"))
            {
                return allLocalRoles.FirstOrDefault(r => r.Name == "REQUISITION_PORTAL_ADMIN");
            }
            if (ssoRoles.Contains("Admin")) // Generic admin role if it exists in SSO
            {
                return allLocalRoles.FirstOrDefault(r => r.Name == "Admin");
            }
            
            // Default to Employee role if no higher role is found
            return allLocalRoles.FirstOrDefault(r => r.Name == "Employee");
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