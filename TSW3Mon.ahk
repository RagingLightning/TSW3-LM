#SingleInstance Force
#Persistent
FileTime := 0
FileGetTime, FileTime, % A_Args[1], M

while 1 {
	Sleep, % A_Args[2]
	FileGetTime, NewTime, % A_Args[1], M
	if (NewTime != FileTime) {
		ExitApp
	}
}