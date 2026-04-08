$base='http://localhost:5042'

function Get-Body($resp){
  if($null -eq $resp){ return '' }
  try {
    $sr=New-Object System.IO.StreamReader($resp.GetResponseStream())
    $txt=$sr.ReadToEnd()
    $sr.Close()
    return $txt
  } catch { return '' }
}

function CallApi($name,$method,$path,$token,$body){
  Write-Host (">> " + $name)
  $uri = "$base$path"
  $headers=@{}
  if($token){ $headers['Authorization']="Bearer $token" }
  $result=[ordered]@{Name=$name; Method=$method; Path=$path; Status=0; Ok=$false; Note=''}
  try {
    if($body){
      $json=$body|ConvertTo-Json -Depth 10
      $r=Invoke-WebRequest -UseBasicParsing -TimeoutSec 20 -Uri $uri -Method $method -Headers $headers -ContentType 'application/json' -Body $json
    } else {
      $r=Invoke-WebRequest -UseBasicParsing -TimeoutSec 20 -Uri $uri -Method $method -Headers $headers
    }
    $result.Status=[int]$r.StatusCode
    $result.Ok=$true
    $result.Note=($r.Content -replace "`r|`n",' ')
    return [pscustomobject]$result
  } catch {
    $resp=$_.Exception.Response
    if($resp){
      $result.Status=[int]$resp.StatusCode
      $result.Note=(Get-Body $resp -replace "`r|`n",' ')
    } else {
      $result.Status=-1
      $result.Note=$_.Exception.Message
    }
    return [pscustomobject]$result
  }
}

$results=@()
$stamp=[DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$customerEmail="customer$stamp@test.local"
$driverEmail="driver$stamp@test.local"
$password='Abcdef1!'
$phoneSuffix = ($stamp % 100000000)
$phone1 = ('09' + $phoneSuffix.ToString().PadLeft(8,'0'))
$phone2 = ('08' + $phoneSuffix.ToString().PadLeft(8,'0'))

$results += CallApi 'Auth.Register.Customer' 'POST' '/api/v1/auth/register' $null @{email=$customerEmail;password=$password;firstName='Cus';lastName='Tom';phone=$phone1}
$results += CallApi 'Auth.Register.DriverUser' 'POST' '/api/v1/auth/register' $null @{email=$driverEmail;password=$password;firstName='Dri';lastName='Ver';phone=$phone2}

$loginCustomer = CallApi 'Auth.Login.Customer' 'POST' '/api/v1/auth/login' $null @{email=$customerEmail;password=$password}
$results += $loginCustomer
$customerToken=$null
$customerRefresh=$null
if($loginCustomer.Ok){
  $obj=$loginCustomer.Note | ConvertFrom-Json
  $customerToken=$obj.accessToken
  $customerRefresh=$obj.refreshToken
}

$loginDriver = CallApi 'Auth.Login.DriverUser' 'POST' '/api/v1/auth/login' $null @{email=$driverEmail;password=$password}
$results += $loginDriver
$driverToken=$null
if($loginDriver.Ok){
  $obj2=$loginDriver.Note | ConvertFrom-Json
  $driverToken=$obj2.accessToken
}

$results += CallApi 'Auth.Refresh' 'POST' '/api/v1/auth/refresh' $null @{refreshToken=$customerRefresh}
$results += CallApi 'Auth.ForgotPassword' 'POST' '/api/v1/auth/forgot-password' $null @{email=$customerEmail}
$results += CallApi 'Auth.ResetPassword.InvalidToken' 'POST' '/api/v1/auth/reset-password' $null @{token='invalid';newPassword='Xyzabc1!'}
$results += CallApi 'Auth.VerifyEmail.InvalidToken' 'POST' '/api/v1/auth/verify-email' $null @{userId='00000000-0000-0000-0000-000000000000';token='invalid'}
$results += CallApi 'Auth.ChangePassword' 'POST' '/api/v1/auth/change-password' $customerToken @{currentPassword=$password;newPassword='Qwerty1!'}

$results += CallApi 'Users.GetMe' 'GET' '/api/v1/users/me' $customerToken $null
$results += CallApi 'Users.UpdateProfile' 'PUT' '/api/v1/users/me/profile' $customerToken @{firstName='Cu';lastName='Stomer';avatarUrl=$null;dateOfBirth=$null;gender='Male';address='HCM'}
$results += CallApi 'Users.Sessions' 'GET' '/api/v1/users/me/sessions' $customerToken $null
$results += CallApi 'Users.RevokeSession.Fake' 'DELETE' '/api/v1/users/me/sessions/11111111-1111-1111-1111-111111111111' $customerToken $null

$results += CallApi 'Rides.Estimate' 'GET' '/api/v1/rides/estimate?pickupLat=10.7769&pickupLng=106.7009&dropoffLat=10.7626&dropoffLng=106.6602&vehicleType=Seat4' $customerToken $null
$createRide = CallApi 'Rides.CreateRequest' 'POST' '/api/v1/rides/request' $customerToken @{pickupLatitude=10.7769;pickupLongitude=106.7009;pickupAddress='Ben Thanh';dropoffLatitude=10.7626;dropoffLongitude=106.6602;dropoffAddress='District 1';vehicleType='Seat4';isPremium=$false}
$results += $createRide
$rideId=$null
if($createRide.Ok){
  try { $rideObj=$createRide.Note | ConvertFrom-Json; $rideId=$rideObj.id } catch {}
}
$results += CallApi 'Rides.Active' 'GET' '/api/v1/rides/active' $customerToken $null
if($rideId){
  $results += CallApi 'Rides.Cancel' 'POST' "/api/v1/rides/$rideId/cancel" $customerToken @{reason='test'}
}

$results += CallApi 'Drivers.Register' 'POST' '/api/v1/drivers/register' $driverToken @{licenseNumber='LIC-123456';licenseExpiry='2030-12-31T00:00:00Z'}
$results += CallApi 'Drivers.AddVehicle' 'POST' '/api/v1/drivers/vehicles' $driverToken @{make='Toyota';model='Vios';year=2022;color='Black';plateNumber=('51A-'+($stamp%100000));vehicleType='Seat4'}
$results += CallApi 'Drivers.Online' 'PUT' '/api/v1/drivers/online' $driverToken @{isOnline=$true}
$results += CallApi 'Drivers.Location' 'PUT' '/api/v1/drivers/location' $driverToken @{latitude=10.7768;longitude=106.7005}
$results += CallApi 'Drivers.Trips' 'GET' '/api/v1/drivers/trips?page=1&pageSize=20' $driverToken $null

$ride2 = CallApi 'Rides.CreateRequest.2' 'POST' '/api/v1/rides/request' $customerToken @{pickupLatitude=10.7769;pickupLongitude=106.7009;pickupAddress='A';dropoffLatitude=10.7626;dropoffLongitude=106.6602;dropoffAddress='B';vehicleType='Seat4';isPremium=$false}
$results += $ride2
$rideId2=$null
if($ride2.Ok){
  try { $rideId2=($ride2.Note|ConvertFrom-Json).id } catch {}
}
$tripId=$null
if($rideId2){
  $accept = CallApi 'Rides.Accept.ByDriver' 'POST' "/api/v1/rides/$rideId2/accept" $driverToken $null
  $results += $accept
  if($accept.Ok){
    try { $tripId=($accept.Note|ConvertFrom-Json).id } catch {}
  }
}
if($tripId){
  $results += CallApi 'Trips.UpdateStatus' 'PUT' "/api/v1/trips/$tripId/status" $driverToken @{status='InProgress'}
  $results += CallApi 'Trips.Complete' 'POST' "/api/v1/trips/$tripId/complete" $driverToken @{actualDistanceKm=4.6}
  $results += CallApi 'Trips.Rate.ByCustomer' 'POST' "/api/v1/trips/$tripId/rate" $customerToken @{rating=5;comment='ok'}
}
$results += CallApi 'Trips.History.Customer' 'GET' '/api/v1/trips/history?page=1&pageSize=20' $customerToken $null
$results += CallApi 'Notifications.GetAll' 'GET' '/api/v1/notifications?page=1&pageSize=20' $customerToken $null
$results += CallApi 'Notifications.MarkRead.Empty' 'PUT' '/api/v1/notifications/read' $customerToken @{notificationIds=@()}
$results += CallApi 'Auth.Logout' 'POST' '/api/v1/auth/logout' $customerToken @{refreshToken=$customerRefresh}
$results += CallApi 'Auth.LogoutAll.Driver' 'POST' '/api/v1/auth/logout-all' $driverToken $null

$results | Select-Object Name,Method,Path,Status,Ok | Format-Table -AutoSize
"`nDETAILS:"
$results | ForEach-Object {
  $msg=$_.Note
  if($msg.Length -gt 220){ $msg=$msg.Substring(0,220) }
  "[$($_.Status)] $($_.Name) -> $msg"
}
