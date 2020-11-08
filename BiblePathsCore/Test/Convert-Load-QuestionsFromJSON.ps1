Param(
        [ValidateScript({Test-Path $_})] 
        [string] $File,
        [switch] $Local
        #[switch] $Production,
        #[switch] $Staging
      )


if ($Production){
    $TargetURI = "https://BiblePathsCore.AzureWebSites.Net"
}

if ($Staging){
    $TargetURI = "https://BiblePathsCorePPE.AzureWebSites.Net"
}

if ($Local){
    $TargetURI = "https://localhost:44387"
}



$JSONFileContent = Get-Content $File 
$JSONVerses = $JSONFileContent | ConvertFrom-JSON


$Questions = @()
$FailedQuestions = @()


<#=====================================================
BEGIN MAIN
=======================================================
#>

# Retrieve questions. 
Write-Host "Retrieving Bible Info" 
$NKJVBible = Invoke-RestMethod -Method Get -Uri "$TargetURI/API/Bibles/NKJV-EN"

Write-host "Selecting Questions"

Foreach ($Verse in $JSONVerses){
    $BookObj = $NKJVBible.BibleBooks | Where-Object {$_.Name.ToLower() -eq $Verse.Book.Tolower()}
    $BookName = $BookObj.Name
    $ChapterNum = [int]$Verse.Chapter
    $VerseNum = [int]$Verse.Verse

    Write-host "Selecting the first Question from $BookName $ChapterNum : $VerseNum"
    $Question = $Verse.Questions[0]

    Write-host "Converting Question for Import"
        $QuestionObj = New-Object -TypeName psobject
        $QuestionObj | Add-Member -MemberType NoteProperty -Name BibleId -value $BookObj.bibleId
        $QuestionObj | Add-Member -MemberType NoteProperty -Name question -value $Question.Question
        $QuestionObj | Add-Member -MemberType NoteProperty -Name points -value $Question.Points
        $QuestionObj | Add-Member -MemberType NoteProperty -Name booknumber -value $BookObj.BookNumber
        $QuestionObj | Add-Member -MemberType NoteProperty -name chapter -value $ChapterNum
        $QuestionObj | Add-Member -MemberType NoteProperty -name startverse -value $VerseNum
        $QuestionObj | Add-Member -MemberType NoteProperty -name endverse -value $VerseNum
        $QuestionObj | Add-member -MemberType NoteProperty -Name owner -value <OWNER-EMAIL>
        $QuestionObj | Add-Member -MemberType NoteProperty -Name source -value "Fill in Blank Script Import 001"

        $AcceptedAnswers = @() # each question can have multiple answers.
        $AcceptedAnswers += $Question.Answer
        $QuestionObj | Add-Member -MemberType NoteProperty -Name Answers -value $AcceptedAnswers 

    $Questions += $QuestionObj
}


# OK now we've got our Questions let's try adding each one to BiblePaths.net/API/PBEQuestions

foreach ($QuestionObj in $Questions)
{
    $JsonString = ConvertTo-Json -InputObject $QuestionObj
    Write-host "Adding Question"
    # We are going to optimistically try to add each question obj 
    try {
        Invoke-RestMethod -Method Post -Uri "$TargetURI/API/QuizQuestions" -Body $JsonString -ContentType "application/json"
    }
    catch {
        # OK this seems to have failed so let's just save out the $QuestionObj 
        $StatusCode =  $_.Exception.Response.StatusCode.value__ 
        $StatusDescription = $_.Exception.Response.StatusDescription
        Write-host " Failed to add question"

        $QuestionObj | Add-Member -MemberType NoteProperty -Name StatusCode -value $StatusCode
        $QuestionObj | Add-Member -MemberType NoteProperty -Name StatusDescription -value $StatusDescription
        $FailedQuestions += $QuestionObj
    }
}



