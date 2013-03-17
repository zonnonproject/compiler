// (be): taken from the Cloo project.

/*

Copyright (c) 2009 - 2011 Fatjon Sakiqi

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/


namespace Zonnon.RTL.OpenCL.Wrapper {
    enum DeviceInfo : int {
        Type = 0x1000,
        VendorId = 0x1001,
        MaxComputeUnits = 0x1002,
        MaxWorkItemDimensions = 0x1003,
        MaxWorkGroupSize = 0x1004,
        MaxWorkItemSizes = 0x1005,
        PreferredVectorWidthChar = 0x1006,
        PreferredVectorWidthShort = 0x1007,
        PreferredVectorWidthInt = 0x1008,
        PreferredVectorWidthLong = 0x1009,
        PreferredVectorWidthFloat = 0x100A,
        PreferredVectorWidthDouble = 0x100B,
        MaxClockFrequency = 0x100C,
        AddressBits = 0x100D,
        MaxReadImageArguments = 0x100E,
        MaxWriteImageArguments = 0x100F,
        MaxMemoryAllocationSize = 0x1010,
        Image2DMaxWidth = 0x1011,
        Image2DMaxHeight = 0x1012,
        Image3DMaxWidth = 0x1013,
        Image3DMaxHeight = 0x1014,
        Image3DMaxDepth = 0x1015,
        ImageSupport = 0x1016,
        MaxParameterSize = 0x1017,
        MaxSamplers = 0x1018,
        MemoryBaseAddressAlignment = 0x1019,
        MinDataTypeAlignmentSize = 0x101A,
        SingleFPConfig = 0x101B,
        GlobalMemoryCacheType = 0x101C,
        GlobalMemoryCachelineSize = 0x101D,
        GlobalMemoryCacheSize = 0x101E,
        GlobalMemorySize = 0x101F,
        MaxConstantBufferSize = 0x1020,
        MaxConstantArguments = 0x1021,
        LocalMemoryType = 0x1022,
        LocalMemorySize = 0x1023,
        ErrorCorrectionSupport = 0x1024,
        ProfilingTimerResolution = 0x1025,
        EndianLittle = 0x1026,
        Available = 0x1027,
        CompilerAvailable = 0x1028,
        ExecutionCapabilities = 0x1029,
        CommandQueueProperties = 0x102A,
        Name = 0x102B,
        Vendor = 0x102C,
        DriverVersion = 0x102D,
        Profile = 0x102E,
        Version = 0x102F,
        Extensions = 0x1030,
        Platform = 0x1031,
        CL_DEVICE_DOUBLE_FP_CONFIG = 0x1032,
        CL_DEVICE_HALF_FP_CONFIG = 0x1033,
        PreferredVectorWidthHalf = 0x1034,
        HostUnifiedMemory = 0x1035,
        NativeVectorWidthChar = 0x1036,
        NativeVectorWidthShort = 0x1037,
        NativeVectorWidthInt = 0x1038,
        NativeVectorWidthLong = 0x1039,
        NativeVectorWidthFloat = 0x103A,
        NativeVectorWidthDouble = 0x103B,
        NativeVectorWidthHalf = 0x103C,
        OpenCLCVersion = 0x103D,
        CL_DEVICE_PARENT_DEVICE_EXT = 0x4054,
        CL_DEVICE_PARITION_TYPES_EXT = 0x4055,
        CL_DEVICE_AFFINITY_DOMAINS_EXT = 0x4056,
        CL_DEVICE_REFERENCE_COUNT_EXT = 0x4057,
        CL_DEVICE_PARTITION_STYLE_EXT = 0x4058
    }
}
