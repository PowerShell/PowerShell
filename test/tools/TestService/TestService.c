#include <windows.h>

#define SERVICE_NAME "TestService"

SERVICE_STATUS_HANDLE hStatus;
SERVICE_STATUS ServiceStatus;

void ControlHandler(DWORD request)
{
	switch(request)
	{
	case SERVICE_CONTROL_STOP:
	case SERVICE_CONTROL_SHUTDOWN:
		ServiceStatus.dwCurrentState = SERVICE_STOPPED;
		SetServiceStatus(hStatus, &ServiceStatus);
		return;
	default:
		break;
	}

	SetServiceStatus(hStatus, &ServiceStatus);
}

void ServiceMain(int argc, char **argv)
{
	ServiceStatus.dwServiceType = SERVICE_WIN32;
	ServiceStatus.dwCurrentState = SERVICE_START_PENDING;
	ServiceStatus.dwControlsAccepted = 
		SERVICE_ACCEPT_STOP |
		SERVICE_ACCEPT_SHUTDOWN;
	ServiceStatus.dwWin32ExitCode = 0;
	ServiceStatus.dwServiceSpecificExitCode = 0;
	ServiceStatus.dwCheckPoint = 0;
	ServiceStatus.dwWaitHint = 0;

	hStatus = RegisterServiceCtrlHandler(SERVICE_NAME, (LPHANDLER_FUNCTION)ControlHandler);
	if(hStatus == (SERVICE_STATUS_HANDLE)0)
	{
		return;
	}

	ServiceStatus.dwCurrentState = SERVICE_RUNNING;
	SetServiceStatus(hStatus, &ServiceStatus);

	while(ServiceStatus.dwCurrentState == SERVICE_RUNNING)
	{
	}

	ServiceStatus.dwCurrentState = SERVICE_STOPPED;
	ServiceStatus.dwWin32ExitCode = -1;
	SetServiceStatus(hStatus, &ServiceStatus);
}

int main(int argc, char **argv)
{
	SERVICE_TABLE_ENTRY ServiceTable[2];
	ServiceTable[0].lpServiceName = SERVICE_NAME;
	ServiceTable[0].lpServiceProc = (LPSERVICE_MAIN_FUNCTION)ServiceMain;

	ServiceTable[1].lpServiceName = NULL;
	ServiceTable[1].lpServiceProc = NULL;

	StartServiceCtrlDispatcher(ServiceTable);
}
