#include <ntddk.h>
#include <wdf.h>
#include <ntstrsafe.h>
#include "Public.h"

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD EvtDeviceAdd;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL EvtIoDeviceControl;

_Use_decl_annotations_
NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath)
{
    WDF_DRIVER_CONFIG config;
    WDF_DRIVER_CONFIG_INIT(&config, EvtDeviceAdd);
    return WdfDriverCreate(DriverObject, RegistryPath, WDF_NO_OBJECT_ATTRIBUTES, &config, WDF_NO_HANDLE);
}

_Use_decl_annotations_
NTSTATUS EvtDeviceAdd(WDFDRIVER Driver, PWDFDEVICE_INIT DeviceInit)
{
    UNREFERENCED_PARAMETER(Driver);

    NTSTATUS status;
    WDFDEVICE device;
    WDF_IO_QUEUE_CONFIG queueConfig;

    DECLARE_CONST_UNICODE_STRING(deviceName, L"\\Device\\KmdfHelloIoctl");
    DECLARE_CONST_UNICODE_STRING(symLink, L"\\DosDevices\\KmdfHelloIoctl");

    status = WdfDeviceInitAssignName(DeviceInit, &deviceName);
    if (!NT_SUCCESS(status)) return status;

    status = WdfDeviceCreateDeviceInterface(DeviceInit, &GUID_DEVINTERFACE_KMDFHELLOIOCTL, NULL);
    if (!NT_SUCCESS(status)) return status;

    status = WdfDeviceCreate(&DeviceInit, WDF_NO_OBJECT_ATTRIBUTES, &device);
    if (!NT_SUCCESS(status)) return status;

    status = WdfDeviceCreateSymbolicLink(device, &symLink);
    if (!NT_SUCCESS(status)) return status;

    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&queueConfig, WdfIoQueueDispatchSequential);
    queueConfig.EvtIoDeviceControl = EvtIoDeviceControl;

    status = WdfIoQueueCreate(device, &queueConfig, WDF_NO_OBJECT_ATTRIBUTES, WDF_NO_HANDLE);
    return status;
}

_Use_decl_annotations_
VOID EvtIoDeviceControl(WDFQUEUE Queue, WDFREQUEST Request, size_t OutputBufferLength, size_t InputBufferLength, ULONG IoControlCode)
{
    UNREFERENCED_PARAMETER(Queue);

    NTSTATUS status = STATUS_INVALID_DEVICE_REQUEST;
    size_t bytesReturned = 0;

    if (IoControlCode == IOCTL_KMDF_HELLO_PING)
    {
        if (InputBufferLength < sizeof(HELLO_PING_REQUEST) || OutputBufferLength < sizeof(HELLO_PING_RESPONSE))
        {
            status = STATUS_BUFFER_TOO_SMALL;
        }
        else
        {
            PHELLO_PING_REQUEST inReq = NULL;
            PHELLO_PING_RESPONSE outRes = NULL;

            status = WdfRequestRetrieveInputBuffer(Request, sizeof(HELLO_PING_REQUEST), (PVOID*)&inReq, NULL);
            if (NT_SUCCESS(status))
            {
                status = WdfRequestRetrieveOutputBuffer(Request, sizeof(HELLO_PING_RESPONSE), (PVOID*)&outRes, NULL);
            }

            if (NT_SUCCESS(status))
            {
                outRes->Version = 1;
                outRes->Nonce = inReq->Nonce;
                outRes->Status = 0;
                bytesReturned = sizeof(HELLO_PING_RESPONSE);
                status = STATUS_SUCCESS;
            }
        }
    }

    WdfRequestCompleteWithInformation(Request, status, bytesReturned);
}
