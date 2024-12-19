# 获取脚本所在的目录
$currentDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
Write-Host "Current Dir：$currentDirectory"
Set-Location -Path $currentDirectory

# 删除 packages 目录下的所有文件
Remove-Item -Path "$currentDirectory\packages\*" -Recurse -Force

# 切换到 ../src 目录
Set-Location -Path "$currentDirectory\..\src"

# 定义一个脚本块
$scriptBlock = {
    param($dir)
    Set-Location $dir
    Write-Host "Executing command in $dir"
    dotnet build
    dotnet pack /p:Version=8.0.0-preview.1 -c Debug --output ../../nupkg/packages
}

# 获取当前目录下的所有子目录
$directories = Get-ChildItem -Directory

# 使用 ForEach-Object -Parallel 来并行执行脚本块
$directories | ForEach-Object -Parallel {
    param($directory)
    $using:scriptBlock.Invoke($directory.FullName)
} -ThrottleLimit 4  # 你可以调整 ThrottleLimit 以设置并行任务的最大数量

Write-Host "Executing full completed!"