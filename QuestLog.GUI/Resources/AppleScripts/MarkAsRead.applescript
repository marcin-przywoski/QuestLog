tell application "Microsoft Outlook"
    try
        set msg to message id __MESSAGE_ID__
        set is read of msg to true
        return "true"
    on error
        return "false"
    end try
end tell
