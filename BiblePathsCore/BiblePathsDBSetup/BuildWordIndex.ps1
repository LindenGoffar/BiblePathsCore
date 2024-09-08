<# 
Description: This script retrieves all verses for a given BibleId along with the existing NoiseWords from the NoiseWords Table and builds an index of all 
Non-Noise words in the given Bible 

This script also uses the contents of stopwords-en.txt to skip Stop/Noise words.
#>

Param(
        [string] $BibleId,
        [switch] $AddWords,
        [switch] $LocalDB,
        [switch] $ProductionDB,
        [switch] $StagingDB,
        [switch] $DevDB
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

if ($DevDB){
    # Dev...
    $Server = "biblepathsdev.database.windows.net"
    $Database = "BiblePathsApp"
    $User = "BiblePathsDevDBA"
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

# Words is a hashtable of format Word=Count
$WordHash = @{}
$WordObjs = @()
$RegExRemovingLeadingPunc = "^[^a-zA-Z]+"
$RegExRemovingTrailingPunc = "[^a-zA-Z]+$"
$WordIndex = 0

# We need to Grab all of the Verses for supplied BibleId
$QueryBibleContentsbyID = @"
            Select * From dbo.BibleVerses
            Where BibleID = '$BibleId'
"@
$BibleContents = Invoke-SqlOnConnection -Connection $SQLConnection -Query $QueryBibleContentsbyID
$VerseCount = $BibleContents.Count

If ($BibleContents.Count -lt 30000 -or $BibleContents.Count -gt 31105){
    Write-Host "BibleContents query returned an incorrect verse count of $VerseCount " -ForegroundColor Red
    Exit 
}

# We need to build a Hash of all of our non-noise words
        $QueryNonNoiseWords = @"
            SELECT * FROM [dbo].BibleNoiseWords
                where isNoise = 0
"@
$NonNoiseWords = Invoke-SqlOnConnection -Connection $SQLConnection -Query $QueryNonNoiseWords 
# Load NonNoiseWords into a Hash
$NonNoiseWordHash = @{}
Foreach ($NonNoiseWord in $NonNoiseWords){
    $NonNoiseWordHash.Add($NonNoiseWord.NoiseWord, $NonNoiseWord.Occurs)
}

$WordsAdded = 0
$BookNumber = 1
$TestamentNumber = 1

foreach ($BibleVerse in $BibleContents){
    $BibleVerseNumber = $BibleVerse.Verse
    $BibleVerseText = $BibleVerse.Text
    $VerseWords = $BibleVerseText.Split(" ")
    foreach ($VerseWord in $VerseWords){
        # strip punctuation
        $VerseWord = $VerseWord.Trim()
        $VerseWord = ($VerseWord -replace $RegExRemovingLeadingPunc,'')
        $VerseWord = ($VerseWord -replace $RegExRemovingTrailingPunc,'')

        # we ignore words < 1 char in length, for obvious reasons.
        # The string "--" is used often to seperate two words i.e. it should be a space
        # we will ignore these words for now as well since they just throw off question generation. 
        if ($VerseWord.Length -ge 1 -and !($VerseWord.Contains("--"))){
        
            if ($NonNoiseWordHash.ContainsKey($VerseWord)){

                if ($AddWords){
                    $TheWord = $VerseWord
                    $VerseID = $BibleVerse.ID
                    $ParameterHash = @{"@Word" = $TheWord}

                    $InsertWord = @"
                                    INSERT INTO BibleWordIndex (BibleID, Word, VerseID)
                                    VALUES ('$BibleID', @Word, '$VerseID')
"@
                    Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertWord -ParameterHash $ParameterHash

                    $WordsAdded++
                    If ($WordsAdded % 1000 -eq 0){
                        write-host "$WordsAdded - Words added to DB"
                    }
                }
            }
        }
    }
}


# Close the SQL Connection... very important not to skip in debugging. 
close-SqlConnection -Connection $SQLConnection

        

