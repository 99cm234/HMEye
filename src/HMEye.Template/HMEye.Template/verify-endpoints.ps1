$baseUrl = "https://localhost:7071/api/plc-data"
$apiKey = "test-api-key"
$headers = @{ "X-Api-Key" = $apiKey }

# Disable SSL validation for localhost
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body
    )
    Write-Host "Testing $Name..." -NoNewline
    try {
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $Url -Headers $headers -Method Get
        } else {
            $json = $Body | ConvertTo-Json -Depth 10
            $response = Invoke-RestMethod -Uri $Url -Headers $headers -Method Post -Body $json -ContentType "application/json"
        }
        Write-Host " OK" -ForegroundColor Green
        return $response
    } catch {
        Write-Host " FAILED" -ForegroundColor Red
        Write-Host $_.Exception.Message
        if ($_.Exception.Response) {
             # Read the error stream
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body: $responseBody"
        }
        return $null
    }
}

# 1. Test Single Read (Regression)
# Assuming 'GVL.bTestBool' exists and is readable
$readResult = Test-Endpoint "Single Read" "GET" "$baseUrl/bool/GVL.bTestBool" $null
if ($readResult) { Write-Host "Read Value: $($readResult.value)" }

# 2. Test Single Write (Regression)
$writeBody = $true
if ($readResult.value -eq $true) { $writeBody = $false }
$writeResult = Test-Endpoint "Single Write" "POST" "$baseUrl/bool/GVL.bTestBool" $writeBody
if ($writeResult.success) { Write-Host "Write Success" }

# 3. Test Bulk Write
$bulkWriteRequest = @{
    items = @(
        @{ address = "GVL.bTestBool"; type = "bool"; value = $true },
        @{ address = "GVL.nTestInt"; type = "int"; value = 123 },
        @{ address = "GVL.sTestString"; type = "string"; value = "Bulk Write Test" }
    )
}
$bulkWriteResult = Test-Endpoint "Bulk Write" "POST" "$baseUrl/bulk-write" $bulkWriteRequest
if ($bulkWriteResult) {
    Write-Host "Bulk Write Results:"
    $bulkWriteResult | ConvertTo-Json
}

# 4. Test Bulk Read (verify write)
$bulkReadRequest = @{
    items = @(
        @{ address = "GVL.bTestBool"; type = "bool" },
        @{ address = "GVL.nTestInt"; type = "int" },
         @{ address = "GVL.sTestString"; type = "string" }
    )
}
$bulkReadResult = Test-Endpoint "Bulk Read" "POST" "$baseUrl/bulk-read" $bulkReadRequest
if ($bulkReadResult) {
    Write-Host "Bulk Read Results:"
    $bulkReadResult | ConvertTo-Json
}
