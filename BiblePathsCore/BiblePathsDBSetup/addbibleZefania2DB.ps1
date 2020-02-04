<# 
Description: Provided a Bible XML file in a defined format, this script pulls the content into 
The Bible Verses table. 

IMPORTANT! This script makes a big assumption on every bible having both the same number of books and books in the same order... this could be a real problem for some bibles/
i.e. if we introduce support for the The Biblical apocrypha... Suggestion is to include these after the new testament that way the concept of "BookNumber" can remain consistent. 
Bottom Line using this Script ORDER IS IMPORTANT!!! 

ALSO IMPORTANT! The Language stored in a Zefania bible is often a 3 letter Code and 
we would prefer you specify the pronouncable name of the languag such as "English" in the language field.

#>

Param(
        [ValidateScript({Test-Path $_})] 
        [string] $File,
        [string] $language,
        [switch] $Validate,
        [switch] $Prod, # Specify when you are ready to update the Production DB. 
        [switch] $BibleExists,
        [switch] $BooksExist
      )

. .\InvokeSQLRemote.ps1


if (!$Validate)
{
    if ($Prod){
        # Cloud DB connection section... 
        $Server = "biblepaths.database.windows.net"
        $Database = "BiblePathsDB"
        $User = "BiblePathsDBA"
    }
    else{
        # Cloud DB connection section... 
        $Server = "biblepathstaging.database.windows.net"
        $Database = "BiblePathStagingDB"
        $User = "StagingDBA"
    }
    $Password = Read-Host "Please Enter the DB Password for User: $User"
    <#
    # Local DB Connection Section... 
    $Server = "(LocalDb)\MSSQLLocalDB"
    $Database = "aspnet-BiblePathBuilder-20160308071813"
    #>
    # Open a SQL Connection

    $SQLConnection = Open-SqlConnection -ServerInstance $Server -Database $Database -Username $User -Password $Password 
}

[XML]$BibleFile = Get-Content $File

$BibleLanguageID = $BibleFile.XMLBIBLE.INFORMATION.language
if ($Language.length -gt 1) {
    $BibleLanguage = $Language
}
else { $BibleLanguage = $BibleLanguageID }
$BibleVersion = $BibleFile.XMLBIBLE.INFORMATION.title
$BibleID = $BibleFile.XMLBIBLE.INFORMATION.identifier + "-" + $BibleFile.XMLBIBLE.INFORMATION.language

if ($Validate){
    Write-Host "BibleID: $BibleID"
    Write-Host "BibleLanguage: $BibleLanguage"
    Write-Host "BibleVersion: $BibleVersion"
}

If (!$BibleExists -and !$Validate){
    $InsertBible = @"
        INSERT INTO Bibles (ID, Language, Version)
        VALUES ('$BibleID', '$BibleLanguage', '$BibleVersion')
"@
    Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertBible
}

ForEach ($BibleBook in $BibleFile.XMLBIBLE.BIBLEBOOK) {
    $BookName = $BibleBook.bname
    if ($BibleBook.CHAPTER.Count -eq $null){
        $BookChapterCount = 1
    }
    else {
        $BookChapterCount = $BibleBook.CHAPTER.Count
    }
    $BookNumber = $BibleBook.bnumber

    Write-Host "$BookNumber - $BookName ($BookChapterCount chapters)"

    if (!$BooksExist -and !$Validate){
        $InsertBibleBook = @"
                INSERT INTO BibleBooks (BibleID, BookNumber, Name, Chapters)
                VALUES ('$BibleID', '$BookNumber', '$BookName', '$BookChapterCount')
"@
        Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertBibleBook
    }
    
    Foreach ($BibleChapter in $BibleBook.CHAPTER) {
        $BibleChapterNumber = $BibleChapter.cnumber
        If ($BookChapterCount > 1){    
            Write-Progress -activity "Adding $BookName " -status "Percent added: " -PercentComplete (($ChapterIndex / $BookChapterCount)  * 100)
        }
        foreach ($BibleVerse in $BibleChapter.VERS) {
            $BibleVerseNumber = $BibleVerse.vnumber
            $BibleVerseText = $BibleVerse.'#text'
            $ParameterHash = @{"@BibleVerseText" = $BibleVerseText}

            if (!$Validate){
                $InsertBibleVerse = @"
                    INSERT INTO BibleVerses (BibleID, Testament, BookNumber, BookName, Chapter, Verse, Text)
                    VALUES ('$BibleID', '$Testament', '$BookNumber', '$BookName', '$BibleChapterNumber', '$BibleVerseNumber', @BibleVerseText)
"@            
                Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertBibleVerse -ParameterHash $ParameterHash
            }
        }
    }
}

if (!$Validate){
    # Close the SQL Connection... very important not to skip in debugging. 
    close-SqlConnection -Connection $SQLConnection
}

        

