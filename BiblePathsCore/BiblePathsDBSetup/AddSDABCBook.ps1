<# 
Description: Provided an SDABC file in a defined format, this script pulls the content into 
The Commentary table. 

#>

Param(
        [ValidateScript({Test-Path $_})] 
        [string] $File,
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

[XML]$CommentaryFile = Get-Content $File

$BibleLanguage = $CommentaryFile.bible.language
$BibleVersion = $CommentaryFile.Bible.translation
$BibleID = $CommentaryFile.bible.id

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

ForEach ($book in $CommentaryFile.bible.book) {
    $bookNumber = $book.number
    $bookName = $book.name
    Write-host "Proccessing $bookName"

    $CommentaryText = $book.text
    $ParameterHash = @{"@CommentaryText" = $CommentaryText}
    $InsertCommentaryBook = @"
                        INSERT INTO CommentaryBooks (BibleID, BookNumber, BookName, Text)
                        VALUES ('$BibleID', '$BookNumber', '$BookName', @CommentaryText)
"@            
                    Invoke-SqlOnConnection -Connection $SQLConnection -Query $InsertCommentaryBook -ParameterHash $ParameterHash
}

# Close the SQL Connection... very important not to skip in debugging. 
close-SqlConnection -Connection $SQLConnection

        

