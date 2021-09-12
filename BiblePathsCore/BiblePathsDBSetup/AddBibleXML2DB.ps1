<# 
Description: Provided a Bible XML file in a defined format, this script pulls the content into 
The Bible Verses table. 

IMPORTANT! This script makes a big assumption on every bible having both the same number of books and books in the same order... this could be a real problem for some bibles/
i.e. if we introduce support for the The Biblical apocrypha... Suggestion is to include these after the new testament that way the concept of "BookNumber" can remain consistent. 
Bottom Line using this Script ORDER IS IMPORTANT!!! 

#>

Param(
        [ValidateScript({Test-Path $_})] 
        [string] $File,
        [switch] $BibleExists,
        [switch] $BooksExist,
        [switch] $ChaptersExist, #Need to port to Zefania
        [Switch] $VersesExist, #Need to port to Zefania
        [Switch] $Validate,
        [Switch] $SkipRefCheck,
        [switch] $LocalDB,
        #[switch] $ProductionDB,
        [switch] $StagingDB
      )

. .\InvokeSQLRemote.ps1

if ($ProductionDB){
    # Production... 
    $Server = "biblepaths.database.windows.net"
    $Database = "BiblePathsDB"
    $User = "BiblePathsDBA"
    $Password = Read-Host "Please Enter the DB Password for User: $User"
    #>
}

if ($StagingDB){
    # Staging...
    $Server = "biblepathsppe.database.windows.net"
    $Database = "BiblePathsPPEDB"
    $User = "BiblePathsPPEDBA"
    $Password = Read-Host "Please Enter the DB Password for User: $User" 
}

if ($LocalDB){
    # Local DB Connection Section... 
    $Server = "(LocalDb)\MSSQLLocalDB"
    $Database = "BiblePathsApp"
    #>
}

# Check whether we have a viable DB now.
if ($Database.Length -lt 1){
    Write-Host "You must specify a target DB using -LocalDB, -StagingDB or -ProductionDB"
    break
}

# Open a SQL Connection 
# We'll do this even on validate to grab the KJV data. 

$SQLConnection = Open-SqlConnection -ServerInstance $Server -Database $Database -Username $User -Password $Password 

$QueryKJVChapterInfo = @"
        SELECT * 
        FROM dbo.BibleChapters
        WHERE BibleID = 'KJV-EN'
"@
$RefChapters = Invoke-SqlOnConnection -Connection $SQLConnection -Query $QueryKJVChapterInfo

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

If (!$BibleExists -and !$Validate){
    $InsertBible = @"
        INSERT INTO Bibles (ID, Language, Version)
        VALUES ('$BibleID', '$BibleLanguage', '$BibleVersion')
"@
    Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertBible
}


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
        Write-Host "Inserting ($BookNumber) $BookName"

        # Let's see if the BookNumber / Chapter Count combination matches our RefChapters Chapter Count i.e. our KJV Bible?
        $RefChapterCount = ($RefChapters | Where-Object {$_.BookNumber -eq $BookNumber} | Measure-Object).count
        
        if (!($BookChapterCount -eq $RefChapterCount) -and !($SkipRefCheck)){
            Write-Host "Chapter Count does not match KJV for Book: $BookName" -ForegroundColor Red
            Exit
        }

        if (!$BooksExist -and !$Validate){
            $InsertBibleBook = @"
                INSERT INTO BibleBooks (BibleID, Testament, TestamentNumber, BookNumber, Name, Chapters)
                VALUES ('$BibleID', '$Testament', '$TestamentNumber', '$BookNumber', '$BookName', '$BookChapterCount')
"@
            Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertBibleBook
        }
        $ChapterIndex = 1
        Foreach ($BibleChapter in $BibleBook.chapter) {
            $BibleChapterNumber = $BibleChapter.number
            $BibleChapterVerseCount = $BibleChapter.verse.Count
            #Write-Host "   Processing Chapter: $BibleChapterNumber" 

            If ($BookChapterCount > 1){    
                Write-Progress -activity "Adding $BookName " -status "Percent added: " -PercentComplete (($ChapterIndex / $BookChapterCount)  * 100)
            }

            # Let's see if our Chapter / Verse Count combination matches our RefChapers verse count. 
            $RefVerseCount = ($Refchapters | Where-Object {($_.BookNumber -eq $BookNumber) -and ($_.ChapterNumber -eq $BibleChapterNumber)}).Verses
            if (!($BibleChapterVerseCount -eq $RefVerseCount) -and !($SkipRefCheck)){
                Write-Host "Verse Count does not match KJV for Book: $BookName and Chapter: $BibleChapterNumber" -ForegroundColor Red
                Exit
            }

            if (!$ChaptersExist -and !$Validate){ # Port this function to Zefania
                $InsertBibleChapter = @"
                    INSERT INTO BibleChapters (BibleID, BookNumber, Name, ChapterNumber, Verses)
                    VALUES ('$BibleID', '$BookNumber', '$BookName', '$BibleChapterNumber', '$BibleChapterVerseCount')
"@
            Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertBibleChapter
                
            }

            foreach ($BibleVerse in $BibleChapter.verse) {
                $BibleVerseNumber = $BibleVerse.number
                $BibleVerseText = $BibleVerse.InnerText

                $ParameterHash = @{"@BibleVerseText" = $BibleVerseText}

                if (!$VersesExist -and !$Validate){ # Port this if to Zefania
                    $InsertBibleVerse = @"
                        INSERT INTO BibleVerses (BibleID, Testament, BookNumber, BookName, Chapter, Verse, Text)
                        VALUES ('$BibleID', '$Testament', '$BookNumber', '$BookName', '$BibleChapterNumber', '$BibleVerseNumber', @BibleVerseText)
"@            
                    Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertBibleVerse -ParameterHash $ParameterHash
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

        

