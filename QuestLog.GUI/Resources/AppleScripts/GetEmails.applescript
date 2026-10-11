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
-- email body cannot forge extra records or shift fields. Loops until stable:
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
    set maxCount to __COUNT__
    set currentCount to 0
    
    -- Iterate through all mail folders named Inbox
    set allInboxes to every mail folder whose name is "Inbox"
    
    repeat with inboxFolder in allInboxes
        if currentCount ≥ maxCount then exit repeat
        try
            set messageList to (messages of inboxFolder __FILTER_CLAUSE__)
            
            repeat with msg in messageList
                if currentCount ≥ maxCount then exit repeat
                
                try
                    set msgId to id of msg as string
                    set msgSubject to subject of msg
                    set msgSender to ""
                    set msgSenderEmail to ""
                    
                    try
                        set msgSender to name of sender of msg
                    end try
                    try
                        set msgSenderEmail to address of sender of msg
                    end try
                    
                    set msgDate to time received of msg as string
                    set msgIsRead to is read of msg as string
                    set msgBody to ""
                    
                    try
                        set msgBody to plain text content of msg
                    on error
                        try
                            set msgBody to content of msg
                        end try
                    end try
                    
                    -- Replace delimiters in content to avoid parsing issues
                    set AppleScript's text item delimiters to return
                    set msgSubject to text items of msgSubject
                    set AppleScript's text item delimiters to " "
                    set msgSubject to msgSubject as string
                    
                    set AppleScript's text item delimiters to return
                    set msgBody to text items of msgBody
                    set AppleScript's text item delimiters to " "
                    set msgBody to msgBody as string
                    set AppleScript's text item delimiters to ""
                    
                    -- Truncate body if too long
                    if (length of msgBody) > 500 then
                        set msgBody to text 1 thru 500 of msgBody & "..."
                    end if
                    
                    -- Strip wire-format separators from attacker-controlled fields
                    set msgSubject to my sanitizeForWire(msgSubject)
                    set msgSender to my sanitizeForWire(msgSender)
                    set msgSenderEmail to my sanitizeForWire(msgSenderEmail)
                    set msgBody to my sanitizeForWire(msgBody)
                    
                    set emailRecord to msgId & "||" & msgSubject & "||" & msgSender & "||" & msgSenderEmail & "||" & msgDate & "||" & msgIsRead & "||" & msgBody
                    
                    if emailList is "" then
                        set emailList to emailRecord
                    else
                        set emailList to emailList & "<<EMAIL>>" & emailRecord
                    end if
                    
                    set currentCount to currentCount + 1
                on error errMsg
                    -- Skip problematic emails
                end try
            end repeat
        end try
    end repeat
    
    return emailList
end tell
