# load for Local Debug
#$TargetURI = "https://localhost:44300"
#TargetURI = "https://biblepathstaging.azurewebsites.net"
$TargetURI = "https://biblepaths.net"

$SelectedBooks = @("Ezra","Nehemiah","Hosea","Jonah","Amos","Micah")

$SourceQuestions = ".\CommentaryQuestions.csv"

$Questions = @()
$QuestionsforImport = @()
$FailedQuestions = @()


<#=====================================================
BEGIN MAIN
=======================================================
#>

# Retrieve questions. 
Write-Host "Retrieving Bible Info" 

$Bibles = Invoke-RestMethod -Method Get -Uri "$TargetURI/API/Bibles"
$NKJVBible = $Bibles | Where-Object {$_.ID -eq "NKJV-EN"}

Write-host "Retrieving Questions from CSV"
$Questions = Import-Csv $SourceQuestions

Write-Host "Adding questions for import" 
Foreach ($Question in $Questions)
{
    if (($Question.Answer.Length -gt 1) -and ($Question.Question.Length -gt 3))
    {
        $QuestionObj = New-Object -TypeName psobject
        $QuestionObj | Add-Member -MemberType NoteProperty -Name Question -value $Question.Question
        $QuestionObj | Add-Member -MemberType NoteProperty -Name Points -value $Question.Points
        $QuestionObj | Add-Member -MemberType NoteProperty -Name BookName -value $Question.BookName
        $QuestionObj | Add-Member -MemberType NoteProperty -name Chapter -value $Question.Chapter
        $QuestionObj | Add-Member -MemberType NoteProperty -name Start_Verse -value $Question.Start_Verse
        $QuestionObj | Add-Member -MemberType NoteProperty -name End_Verse -value $Question.End_Verse
        $QuestionObj | Add-member -MemberType NoteProperty -Name Owner -value "lgoffar@gmail.com"

        $AcceptedAnswers = @() # each question can have multiple answers so it's an array, but in this case it wil have one. 
        $AcceptedAnswers += $Question.Answer
        $QuestionObj | Add-Member -MemberType NoteProperty -Name QuizAnswers -value $AcceptedAnswers 

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
        Invoke-RestMethod -Method Post -Uri "$TargetURI/API/PBEQuestions/AddQuestion" -Body $JsonString -ContentType "application/json"
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



