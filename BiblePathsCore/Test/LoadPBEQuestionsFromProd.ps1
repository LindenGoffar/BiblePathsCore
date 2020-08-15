# load for Local Debug
$TargetURI = "https://localhost:44387"
#$TargetURI = "https://biblepathstaging.azurewebsites.net"

$SourceURI = "https://biblepaths.net"
#$SourceURI = "https://biblepathstaging.azurewebsites.net"

$SelectedBooks = @("Ezra","Nehemiah","Hosea","Jonah","Amos","Micah","Hebrews","James","1 Peter", "2 Peter")


$Questions = @()
$QuestionsforImport = @()
$FailedQuestions = @()


<#=====================================================
BEGIN MAIN
=======================================================
#>

# Retrieve questions. 
Write-Host "Retrieving Bible Info" 
$NKJVBible = Invoke-RestMethod -Method Get -Uri "$SourceURI/API/Bibles/NKJV-EN"

Write-host "Retrieving Questions"
Foreach ($Book in $SelectedBooks){
    $BookObj = $NKJVBible.BibleBooks | Where-Object {$_.Name.ToLower() -eq $Book.Tolower()}
    $ChapterCount = $BookObj.Chapters
    $BookName = $BookObj.Name

    Write-host "Processing $BookName with $ChapterCount Chapters"
    For ($i=1; $i -le $ChapterCount; $i++){
        $Questions += Invoke-RestMethod -Method Get -Uri "$SourceURI/API/PBEQuestions/GetQuestions/?BookName=$Book&Chapter=$i"
    }
}

Write-Host "Adding questions for import" 
Foreach ($Question in $Questions)
{
    if (($Question.QuizAnswers.Count -gt 0) -and ($Question.Question.Length -gt 3))
    {
        $QuestionObj = New-Object -TypeName psobject
        $QuestionObj | Add-Member -MemberType NoteProperty -Name BibleId -value "NKJV-EN"
        $QuestionObj | Add-Member -MemberType NoteProperty -Name question -value $Question.Question
        $QuestionObj | Add-Member -MemberType NoteProperty -Name points -value $Question.Points
        $QuestionObj | Add-Member -MemberType NoteProperty -Name booknumber -value $Question.BookNumber
        $QuestionObj | Add-Member -MemberType NoteProperty -name chapter -value $Question.Chapter
        $QuestionObj | Add-Member -MemberType NoteProperty -name startverse -value $Question.Start_Verse
        $QuestionObj | Add-Member -MemberType NoteProperty -name endverse -value $Question.End_Verse
        $QuestionObj | Add-member -MemberType NoteProperty -Name owner -value "linden@Goffar.com"
        $QuestionObj | Add-Member -MemberType NoteProperty -Name source -value "API Test"

        $AcceptedAnswers = @() # each question can have multiple answers.
        foreach ($Answer in $Question.QuizAnswers)
        {
            $AcceptedAnswers += $Answer
        }
        $QuestionObj | Add-Member -MemberType NoteProperty -Name Answers -value $AcceptedAnswers 

        # Finally add our question object
        $QuestionsforImport += $QuestionObj
    }     
}

# OK now we've got our Questions let's try adding each one to BiblePaths.net/API/PBEQuestions

foreach ($QuestionObj in $QuestionsforImport)
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



