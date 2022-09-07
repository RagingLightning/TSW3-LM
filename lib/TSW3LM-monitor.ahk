#Persistent
FolderPath := A_Args[1]   ; set the folder to watch
interval := A_Args[2]     ; set the interval

SetFileMonitoring(FolderPath, interval)

SetFileMonitoring(FolderPath, interval)  {
   static winmgmts := ComObjGet("winmgmts:"), createSink
   
   SplitPath, FolderPath,,,,, Drive
   Folder := RegExReplace(FolderPath, "[A-Z]:\\|((?<!\\)\\(?!\\)|(?<!\\)$)", "\\")

   ComObjConnect(createSink := ComObjCreate("WbemScripting.SWbemSink"), "FileEvent_")

   winmgmts.ExecNotificationQueryAsync(createSink
      , "Select * From __InstanceOperationEvent"
      . " within " interval
      . " Where Targetinstance Isa 'CIM_DataFile'"
      . " And TargetInstance.Drive='" Drive "'"
      . " And TargetInstance.Path='" Folder "'")
}
   
FileEvent_OnObjectReady(objEvent)
{
   if (objEvent.Path_.Class = "__InstanceModificationEvent" && RegExReplace(objEvent.TargetInstance.Name, ".*\\") = "UGCLiveries_0.sav")
      ExitApp
}
