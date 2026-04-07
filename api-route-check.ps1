$base='http://localhost:5042'

function CallCurl($name,$method,$path,$body){
  $tmp = Join-Path $PWD ("tmp_" + [guid]::NewGuid().ToString() + ".txt")
  $url = "$base$path"
  $args = @('-sS','-m','15','-o',$tmp,'-w','%{http_code}','-X',$method)
  if($body){
    $json = $body | ConvertTo-Json -Depth 10 -Compress
    $args += @('-H','Content-Type: application/json','-d',$json)
  }
  $args += $url

  $code = & curl.exe @args
  $content = ''
  if(Test-Path $tmp){
    $content = Get-Content $tmp -Raw
    Remove-Item $tmp -Force
  }
  $short = $content -replace "`r|`n",' '
  if($short.Length -gt 180){ $short = $short.Substring(0,180) }
  [pscustomobject]@{Name=$name; Method=$method; Path=$path; Status=$code; Sample=$short}
}

$stamp=[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$email="routecheck$stamp@test.local"
$pwd='Abcdef1!'
$phone=('09'+($stamp % 100000000).ToString().PadLeft(8,'0'))

$tests = @()
# Auth public
$tests += CallCurl 'Auth.Register' 'POST' '/api/v1/auth/register' @{email=$email;password=$pwd;firstName='Api';lastName='Check';phone=$phone}
$tests += CallCurl 'Auth.Login' 'POST' '/api/v1/auth/login' @{email=$email;password=$pwd}
$tests += CallCurl 'Auth.Refresh.NoToken' 'POST' '/api/v1/auth/refresh' @{refreshToken=''}
$tests += CallCurl 'Auth.VerifyEmail.POST' 'POST' '/api/v1/auth/verify-email' @{userId='00000000-0000-0000-0000-000000000000';token='invalid'}
$tests += CallCurl 'Auth.VerifyEmail.GET' 'GET' '/api/v1/auth/verify-email?userId=00000000-0000-0000-0000-000000000000&token=invalid' $null
$tests += CallCurl 'Auth.ForgotPassword' 'POST' '/api/v1/auth/forgot-password' @{email=$email}
$tests += CallCurl 'Auth.ResetPassword' 'POST' '/api/v1/auth/reset-password' @{token='invalid';newPassword='Xyzabc1!'}

# Auth protected
$tests += CallCurl 'Auth.ChangePassword' 'POST' '/api/v1/auth/change-password' @{currentPassword='a';newPassword='b'}
$tests += CallCurl 'Auth.Logout' 'POST' '/api/v1/auth/logout' @{refreshToken='x'}
$tests += CallCurl 'Auth.LogoutAll' 'POST' '/api/v1/auth/logout-all' $null

# Users
$tests += CallCurl 'Users.Me' 'GET' '/api/v1/users/me' $null
$tests += CallCurl 'Users.Profile' 'PUT' '/api/v1/users/me/profile' @{firstName='A'}
$tests += CallCurl 'Users.Sessions' 'GET' '/api/v1/users/me/sessions' $null
$tests += CallCurl 'Users.RevokeSession' 'DELETE' '/api/v1/users/me/sessions/11111111-1111-1111-1111-111111111111' $null

# Drivers
$tests += CallCurl 'Drivers.Register' 'POST' '/api/v1/drivers/register' @{licenseNumber='LIC-1';licenseExpiry='2030-12-31T00:00:00Z'}
$tests += CallCurl 'Drivers.AddVehicle' 'POST' '/api/v1/drivers/vehicles' @{make='T';model='M';year=2022;color='Black';plateNumber='51A-11111';vehicleType='Economy'}
$tests += CallCurl 'Drivers.Online' 'PUT' '/api/v1/drivers/online' @{isOnline=$true}
$tests += CallCurl 'Drivers.Location' 'PUT' '/api/v1/drivers/location' @{latitude=10.7;longitude=106.7}
$tests += CallCurl 'Drivers.Nearby' 'GET' '/api/v1/drivers/nearby?lat=10.7&lng=106.7&radius=5&vehicleType=Economy' $null
$tests += CallCurl 'Drivers.Trips' 'GET' '/api/v1/drivers/trips?page=1&pageSize=20' $null

# Rides
$tests += CallCurl 'Rides.Estimate' 'GET' '/api/v1/rides/estimate?pickupLat=10.7&pickupLng=106.7&dropoffLat=10.8&dropoffLng=106.8&vehicleType=Economy' $null
$tests += CallCurl 'Rides.Request' 'POST' '/api/v1/rides/request' @{pickupLatitude=10.7;pickupLongitude=106.7;pickupAddress='A';dropoffLatitude=10.8;dropoffLongitude=106.8;dropoffAddress='B';vehicleType='Economy'}
$tests += CallCurl 'Rides.Accept' 'POST' '/api/v1/rides/11111111-1111-1111-1111-111111111111/accept' $null
$tests += CallCurl 'Rides.Cancel' 'POST' '/api/v1/rides/11111111-1111-1111-1111-111111111111/cancel' @{reason='x'}
$tests += CallCurl 'Rides.Active' 'GET' '/api/v1/rides/active' $null

# Trips
$tests += CallCurl 'Trips.UpdateStatus' 'PUT' '/api/v1/trips/11111111-1111-1111-1111-111111111111/status' @{status='InProgress'}
$tests += CallCurl 'Trips.Complete' 'POST' '/api/v1/trips/11111111-1111-1111-1111-111111111111/complete' @{actualDistanceKm=4.5}
$tests += CallCurl 'Trips.Rate' 'POST' '/api/v1/trips/11111111-1111-1111-1111-111111111111/rate' @{rating=5;comment='ok'}
$tests += CallCurl 'Trips.History' 'GET' '/api/v1/trips/history?page=1&pageSize=20' $null

# Notifications
$tests += CallCurl 'Notifications.Get' 'GET' '/api/v1/notifications?page=1&pageSize=20' $null
$tests += CallCurl 'Notifications.Read' 'PUT' '/api/v1/notifications/read' @{notificationIds=@()}

$tests | Format-Table -AutoSize
"`nDETAILS"
$tests | ForEach-Object { "[$($_.Status)] $($_.Name): $($_.Sample)" }
