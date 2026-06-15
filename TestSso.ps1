$body = @{ Username = "test" } | ConvertTo-Json
$response = Invoke-WebRequest -Uri "https://sso-app-dev.digitvant.com/api/users/initiate-reset" -Method Post -Body $body -ContentType "application/json" -SkipHttpErrorCheck
Write-Output "Status: $($response.StatusCode)"
Write-Output "Content: $($response.Content)"
