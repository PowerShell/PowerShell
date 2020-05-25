# Proxy servers in format: https://<url>:<port>. Can be multiple Proxy servers used by comma separation
$proxy_servers = "proxy-rwe-de.energy.local:8080", "proxy-rwe-uk.energy.local:8080", "rwestproxy-singapore.rwe.com:8080";
# 

# URL's to be tested thru Proxy.
$urls = "https://de.reuters.com", "https://www.bbc.com", "https://www.bloomberg.com", "https://maps.google.com/";
# 

# ComputerName of tested machine
Write-Host "Testing script on machine: " $env:COMPUTERNAME
function DoWork {   
    # Test URL's without PROXY first
    foreach ($url in $urls) {
        TestWithoutProxy -URL $url
    }
    # Test URL's with Proxy 
    foreach ($proxy_serv in $proxy_servers) {
        foreach ($url in $urls) {
            TestProxy -ProxyServer $proxy_serv -URL $url
        }
    }
}
function TestWithoutProxy {
    param (
        [Parameter(Mandatory = $true,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            Position = 0)]
        [Alias("U")] [string]$url
    )
    $WebClientWithoutProxy = New-Object System.Net.WebClient
    Write-Host -NoNewLine "Testing without proxy server " -ForegroundColor Yellow
    Write-Host -NoNewLine " [*] URL test without Proxy -" $url -ForegroundColor White 
    Try { 
        $contentWithouProxy = $WebClientWithoutProxy.DownloadString($url)
        Write-Host -NoNewline " [+] Opened $url successfully without Proxy"
    }
    catch {
        Write-Host -NoNewLine " [-] Unable to access $url without Proxy" -ForegroundColor Red 
    }
    Write-Host " [*]"
}
function TestProxy {
    param (
        [Parameter(Mandatory = $true,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            Position = 0)]
        [Alias("CN")][string[]]$ProxyServer, [Parameter(Position = 1)]
        [Alias("U")] [string]$url
    )
    $proxy = new-object System.Net.WebProxy($ProxyServer)
    $WebClient = new-object System.Net.WebClient
    $WebClient.proxy = $proxy
    #Write-Host ""
    Write-Host -NoNewLine "Testing proxy server: " $ProxyServer -ForegroundColor Yellow 
    Write-Host -NoNewline " [*] URL test -" $url -ForeGroundColor White
    Try {
        $content = $WebClient.DownloadString($url)
        Write-Host -NoNewLine " [+] Opened $url successfully" -ForegroundColor Green "- Amount of chars found on website" $content.Length
    }
    catch {
        Write-Host -NoNewLine  " [-] Unable to access $url" -ForegroundColor Red 
    } 
    Write-Host " [*]"
}
DoWork