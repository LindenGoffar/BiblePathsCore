<# 
Description: Pulls all words in Word Index > 230,000 words, and adds a Random Integer as a means of randomizing but still limiting results required for the Game. 

THIS IS A VERY EXPENSIVE WAY... do this instead

UPDATE BibleWordIndex
SET    RandomInt = abs(checksum(NewId()) % 50)

#>

Param(
        [switch] $LocalDB,
        [switch] $ProductionDB,
        [switch] $StagingDB
      )

. .\InvokeSQLRemote.ps1

if ($ProductionDB){
    # Production... 
    $Server = "biblepaths.database.windows.net"
    $Database = "BiblePathsDB"
    $User = "BiblePathsDBA"
    $Password = Read-Host "Please Enter the DB Password for User: $User"
}

if ($StagingDB){
    # Staging... 
    $Server = "biblepathstaging.database.windows.net"
    $Database = "BiblePathStagingDB"
    $User = "StagingDBA"
    $Password = Read-Host "Please Enter the DB Password for User: $User"
}

if ($LocalDB){
    # Local DB Connection Section... 
    $Server = "(LocalDb)\MSSQLLocalDB"
    $Database = "BiblePathsApp"
}

# Check whether we have a viable DB now.
if ($Database.Length -lt 1){
    Write-Host "You must specify a target DB using -LocalDB, -StagingDB or -ProductionDB"
    break
}

# Open a SQL Connection 
$SQLConnection = Open-SqlConnection -ServerInstance $Server -Database $Database -Username $User -Password $Password 


# We need to Grab all of the Word Indices 
$QueryWordIndices = @"
            Select * from dbo.BibleWordIndex WITH (NOLOCK)
"@
$WordIndices = Invoke-SqlOnConnection -Connection $SQLConnection -Query $QueryWordIndices

$IndexCount = $WordIndices.count

$WordsUpdated = 0

foreach ($WordIndex in $Wordindices){
    
    $RandomNum = Get-Random -Minimum 1 -Maximum 50
    $WordID = $WordIndex.ID

    $UpdateWordQuery = @"
                    Update dbo.BibleWordIndex 
                    Set RandomInt = $RandomNum
                    Where ID = $WordID
"@
    Invoke-SqlOnConnection -Connection $SQLConnection -Query $UpdateWordQuery 

    $WordsUpdated++
    If ($WordsUpdated % 1000 -eq 0){
        write-host "$WordsUpdated - Words Updated in DB"
    }
}


# Close the SQL Connection... very important not to skip in debugging. 
close-SqlConnection -Connection $SQLConnection

        
