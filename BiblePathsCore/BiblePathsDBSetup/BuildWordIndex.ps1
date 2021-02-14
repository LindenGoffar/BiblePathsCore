<# 
Description: Provided a Bible XML file in a defined format, this script pulls the content and creates an index of Word to BibleVerse mapping 

This script also uses the contents of stopwords-en.txt to skip Stop/Noise words.
#>

Param(
        [ValidateScript({Test-Path $_})] 
        [string] $File,
        [switch] $AddWords,
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

# Words is a hashtable of format Word=Count
$WordHash = @{}
$WordObjs = @()
$RegExRemovingLeadingPunc = "^[^a-zA-Z]+"
$RegExRemovingTrailingPunc = "[^a-zA-Z]+$"
$WordIndex = 0

[XML]$BibleFile = Get-Content $File

$BibleLanguage = $BibleFile.bible.language
$BibleVersion = $BibleFile.Bible.translation
$BibleID = $Biblefile.bible.id

If ($BibleLanguage.Length -gt 1){
    # $BibleObJ | Add-Member -NotePropertyName "Language" -NotePropertyValue $BibleLanguage -Force
}
Else {
    Write-Host "Failure to obtain Bible Language" -ForegroundColor Red
    Exit 
}
If ($BibleVersion.Length -gt 1){
    # $BibleObJ | Add-Member -NotePropertyName "Version" -NotePropertyValue $BibleVersion -Force
}
Else {
    Write-Host "Failure to obtain Bible Version" -ForegroundColor Red
    Exit
}
If ($BibleID.Length -gt 1){
    # $BibleObJ | Add-Member -NotePropertyName "ID" -NotePropertyValue $BibleID -Force
}
Else {
    Write-Host "Failure to obtain Bible ID" -ForegroundColor Red
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
ForEach ($BibleTestament in $BibleFile.bible.testament) {
    $Testament = $BibleTestament.name
    $TestamentBookCount = $BibleTestament.book.count
    Write-host "Proccessing $TestamentBookCount books in $Testament"

    ForEach ($BibleBook in $BibleTestament.book) {
        $BookName = $BibleBook.name 
        # Need to handle 1 chapter books so we use measure-object
        $BookChapterCount = ($BibleBook.chapter | Measure-Object).count
        Write-Host "Inspecting ($BookNumber) $BookName"

        $ChapterIndex = 1
        Foreach ($BibleChapter in $BibleBook.chapter) {
            $BibleChapterNumber = $BibleChapter.number
            $BibleChapterVerseCount = $BibleChapter.verse.Count
            #Write-Host "   Processing Chapter: $BibleChapterNumber" 

            If ($BookChapterCount > 1){    
                Write-Progress -activity "Inspecting $BookName " -status "Percent processed: " -PercentComplete (($ChapterIndex / $BookChapterCount)  * 100)
            }

            foreach ($BibleVerse in $BibleChapter.verse) {
                $BibleVerseNumber = $BibleVerse.number
                $BibleVerseText = $BibleVerse.InnerText

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
                        #Write-Host $VerseWord
                        if ($NonNoiseWordHash.ContainsKey($VerseWord)){

                            if ($AddWords){
                                $TheWord = $VerseWord
                                $Chapter = $word.Chapter
                                $Verse = $word.Verse

                                $ParameterHash = @{"@Word" = $TheWord}

                                $InsertWord = @"
                                    INSERT INTO BibleWordIndex (BibleID, Word, BookNumber, Chapter, Verse)
                                        VALUES ('$BibleID', @Word, '$BookNumber', '$BibleChapterNumber', '$BibleVerseNumber')
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
            $ChapterIndex++
        }
        $BookNumber++   
    }
    $TestamentNumber++
}



# Close the SQL Connection... very important not to skip in debugging. 
close-SqlConnection -Connection $SQLConnection

        

