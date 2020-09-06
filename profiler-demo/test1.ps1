function a {
    "me"
}

function ff() {
    "hello" 
    "this" 
    "is"
    "me"
}

if ($true) { 
    "hit"
}
else { 
    "not-hit"
}

$a = @()

foreach ($i in 1..10) {
    $a += @(100)
    Start-Sleep -Milliseconds 100
} 

[Threading.Thread]::Sleep(1000)

ff
ff
ff

return