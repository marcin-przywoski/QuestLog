-- Remove a literal token from a value (used to strip wire-format delimiters).
on stripToken(valueText, token)
    set AppleScript's text item delimiters to token
    set textParts to text items of (valueText as string)
    set AppleScript's text item delimiters to ""
    set stripped to textParts as string
    set AppleScript's text item delimiters to ""
    return stripped
end stripToken

-- Strip the record/field separators from attacker-controlled text so a crafted
-- email cannot forge extra records or shift fields. Loops until stable:
-- removing one token can create the other (e.g. "<<E||MAIL>>").
on sanitizeForWire(valueText)
    set sanitized to valueText as string
    repeat
        set previous to sanitized
        set sanitized to my stripToken(sanitized, "<<EMAIL>>")
        set sanitized to my stripToken(sanitized, "||")
        if sanitized is previous then exit repeat
    end repeat
    return sanitized
end sanitizeForWire

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
        set AppleScript's text item delimiters to ""
        
        set msgSubject to my sanitizeForWire(msgSubject)
        set msgSender to my sanitizeForWire(msgSender)
        set msgSenderEmail to my sanitizeForWire(msgSenderEmail)
        set msgBody to my sanitizeForWire(msgBody)
        
        set emailList to msgId & "||" & msgSubject & "||" & msgSender & "||" & msgSenderEmail & "||" & msgDate & "||" & msgIsRead & "||" & msgBody
    end try
    return emailList
end tell
