tell application "Microsoft Outlook"
    set emailList to ""
    try
        set msg to message id __MESSAGE_ID__
        set msgId to id of msg as string
        set msgSubject to subject of msg
        set msgSender to name of sender of msg
        set msgSenderEmail to address of sender of msg
        set msgDate to time received of msg as string
        set msgIsRead to is read of msg as string
        set msgBody to plain text content of msg
        
        set AppleScript's text item delimiters to return
        set msgSubject to text items of msgSubject
        set AppleScript's text item delimiters to " "
        set msgSubject to msgSubject as string
        
        set AppleScript's text item delimiters to return
        set msgBody to text items of msgBody
        set AppleScript's text item delimiters to " "
        set msgBody to msgBody as string
        
        set emailList to msgId & "||" & msgSubject & "||" & msgSender & "||" & msgSenderEmail & "||" & msgDate & "||" & msgIsRead & "||" & msgBody
    end try
    return emailList
end tell
