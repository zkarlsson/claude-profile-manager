function Invoke-ClaudeProfileCLI {
    <#
    .SYNOPSIS
        Executes the Claude Profile Manager CLI with specified arguments.
    
    .DESCRIPTION
        Invokes the Claude Profile Manager CLI executable with the provided arguments,
        handles output parsing, and provides proper error handling for PowerShell integration.
    
    .PARAMETER Arguments
        Array of arguments to pass to the CLI executable.
    
    .PARAMETER ParseJsonOutput
        Switch to indicate that the CLI output should be parsed as JSON.
    
    .PARAMETER ThrowOnError
        Switch to indicate that non-zero exit codes should throw exceptions.
        Default is $true.
    
    .OUTPUTS
        System.Object
        Returns the CLI output, optionally parsed as JSON objects.
    
    .EXAMPLE
        $profiles = Invoke-ClaudeProfileCLI -Arguments @('list') -ParseJsonOutput
        
    .EXAMPLE
        Invoke-ClaudeProfileCLI -Arguments @('save', 'work-profile')
    #>
    
    [CmdletBinding()]
    [OutputType([object])]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        
        [Parameter(Mandatory = $false)]
        [switch]$ParseJsonOutput,
        
        [Parameter(Mandatory = $false)]
        [switch]$ThrowOnError = $true
    )
    
    try {
        # Get CLI executable path
        $cliPath = Get-ClaudeProfileCLIPath
        
        # Prepare arguments with proper escaping
        $escapedArgs = $Arguments | ForEach-Object {
            if ($_ -match '\s') {
                # Quote arguments that contain spaces
                "`"$_`""
            }
            else {
                $_
            }
        }
        
        Write-Verbose "Executing CLI: $cliPath $($escapedArgs -join ' ')"
        
        # Execute CLI and capture output
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo.FileName = $cliPath
        $process.StartInfo.Arguments = $escapedArgs -join ' '
        $process.StartInfo.UseShellExecute = $false
        $process.StartInfo.RedirectStandardOutput = $true
        $process.StartInfo.RedirectStandardError = $true
        $process.StartInfo.CreateNoWindow = $true
        $process.StartInfo.WorkingDirectory = Get-Location
        
        # Start process
        [void]$process.Start()
        
        # Read output
        $stdout = $process.StandardOutput.ReadToEnd()
        $stderr = $process.StandardError.ReadToEnd()
        
        # Wait for completion
        $process.WaitForExit()
        $exitCode = $process.ExitCode
        
        Write-Verbose "CLI exit code: $exitCode"
        if ($stdout) { Write-Verbose "CLI stdout: $stdout" }
        if ($stderr) { Write-Verbose "CLI stderr: $stderr" }
        
        # Handle errors
        if ($exitCode -ne 0) {
            $errorMessage = "Claude Profile Manager CLI failed with exit code $exitCode"
            if ($stderr) {
                $errorMessage += ": $stderr"
            }
            elseif ($stdout) {
                $errorMessage += ": $stdout"
            }
            
            if ($ThrowOnError) {
                throw [System.InvalidOperationException]::new($errorMessage)
            }
            else {
                Write-Warning $errorMessage
                return $null
            }
        }
        
        # Handle successful output
        if (-not $stdout) {
            return $null
        }
        
        # Parse JSON output if requested
        if ($ParseJsonOutput) {
            try {
                $jsonOutput = $stdout | ConvertFrom-Json
                return $jsonOutput
            }
            catch {
                Write-Warning "Failed to parse CLI output as JSON: $($_.Exception.Message)"
                Write-Verbose "Raw output was: $stdout"
                
                # Fallback to raw output
                return $stdout
            }
        }
        
        # Return raw output
        return $stdout.Trim()
    }
    catch {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    finally {
        # Cleanup process if it exists
        if ($process -and -not $process.HasExited) {
            try {
                $process.Kill()
                $process.WaitForExit(5000)  # Wait up to 5 seconds
            }
            catch {
                Write-Warning "Failed to clean up CLI process: $($_.Exception.Message)"
            }
        }
        
        if ($process) {
            $process.Dispose()
        }
    }
}