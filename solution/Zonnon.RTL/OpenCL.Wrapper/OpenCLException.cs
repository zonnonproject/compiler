// (be): taken from the Cloo project.

/*

Copyright (c) 2009 - 2010 Fatjon Sakiqi

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
    using System;
    using System.Diagnostics;

    public abstract class OpenCLException : Exception {
        internal ReturnCode ReturnCode { get; private set; }

        internal OpenCLException(ReturnCode code)
            : base("OpenCL error: " + code.ToString()) {
            ReturnCode = code;
        }

        [DebuggerStepThrough]
        internal static void ThrowOnError(ReturnCode errorCode) {
            switch (errorCode) {
                case ReturnCode.Success:
                return;

                case ReturnCode.DeviceNotFound:
                throw new DeviceNotFoundOpenCLException();

                case ReturnCode.DeviceNotAvailable:
                throw new DeviceNotAvailableOpenCLException();

                case ReturnCode.CompilerNotAvailable:
                throw new CompilerNotAvailableOpenCLException();

                case ReturnCode.MemoryObjectAllocationFailure:
                throw new MemoryObjectAllocationFailureOpenCLException();

                case ReturnCode.OutOfResources:
                throw new OutOfResourcesOpenCLException();

                case ReturnCode.OutOfHostMemory:
                throw new OutOfHostMemoryOpenCLException();

                case ReturnCode.ProfilingInfoNotAvailable:
                throw new ProfilingInfoNotAvailableOpenCLException();

                case ReturnCode.MemoryCopyOverlap:
                throw new MemoryCopyOverlapOpenCLException();

                case ReturnCode.ImageFormatMismatch:
                throw new ImageFormatMismatchOpenCLException();

                case ReturnCode.ImageFormatNotSupported:
                throw new ImageFormatNotSupportedOpenCLException();

                case ReturnCode.BuildProgramFailure:
                throw new BuildProgramFailureOpenCLException(null);

                case ReturnCode.MapFailure:
                throw new MapFailureOpenCLException();

                case ReturnCode.InvalidValue:
                throw new InvalidValueOpenCLException();

                case ReturnCode.InvalidDeviceType:
                throw new InvalidDeviceTypeOpenCLException();

                case ReturnCode.InvalidPlatform:
                throw new InvalidPlatformOpenCLException();

                case ReturnCode.InvalidDevice:
                throw new InvalidDeviceOpenCLException();

                case ReturnCode.InvalidContext:
                throw new InvalidContextOpenCLException();

                case ReturnCode.InvalidCommandQueueFlags:
                throw new InvalidCommandQueueFlagsOpenCLException();

                case ReturnCode.InvalidCommandQueue:
                throw new InvalidCommandQueueOpenCLException();

                case ReturnCode.InvalidHostPointer:
                throw new InvalidHostPointerOpenCLException();

                case ReturnCode.InvalidMemoryObject:
                throw new InvalidMemoryObjectOpenCLException();

                case ReturnCode.InvalidImageFormatDescriptor:
                throw new InvalidImageFormatDescriptorOpenCLException();

                case ReturnCode.InvalidImageSize:
                throw new InvalidImageSizeOpenCLException();

                case ReturnCode.InvalidSampler:
                throw new InvalidSamplerOpenCLException();

                case ReturnCode.InvalidBinary:
                throw new InvalidBinaryOpenCLException();

                case ReturnCode.InvalidBuildOptions:
                throw new InvalidBuildOptionsOpenCLException();

                case ReturnCode.InvalidProgram:
                throw new InvalidProgramOpenCLException();

                case ReturnCode.InvalidProgramExecutable:
                throw new InvalidProgramExecutableOpenCLException();

                case ReturnCode.InvalidKernelName:
                throw new InvalidKernelNameOpenCLException();

                case ReturnCode.InvalidKernelDefinition:
                throw new InvalidKernelDefinitionOpenCLException();

                case ReturnCode.InvalidKernel:
                throw new InvalidKernelOpenCLException();

                case ReturnCode.InvalidArgumentIndex:
                throw new InvalidArgumentIndexOpenCLException();

                case ReturnCode.InvalidArgumentValue:
                throw new InvalidArgumentValueOpenCLException();

                case ReturnCode.InvalidArgumentSize:
                throw new InvalidArgumentSizeOpenCLException();

                case ReturnCode.InvalidKernelArguments:
                throw new InvalidKernelArgumentsOpenCLException();

                case ReturnCode.InvalidWorkDimension:
                throw new InvalidWorkDimensionsOpenCLException();

                case ReturnCode.InvalidWorkGroupSize:
                throw new InvalidWorkGroupSizeOpenCLException();

                case ReturnCode.InvalidWorkItemSize:
                throw new InvalidWorkItemSizeOpenCLException();

                case ReturnCode.InvalidGlobalOffset:
                throw new InvalidGlobalOffsetOpenCLException();

                case ReturnCode.InvalidEventWaitList:
                throw new InvalidEventWaitListOpenCLException();

                case ReturnCode.InvalidEvent:
                throw new InvalidEventOpenCLException();

                case ReturnCode.InvalidOperation:
                throw new InvalidOperationOpenCLException();

                case ReturnCode.InvalidGLObject:
                throw new InvalidGLObjectOpenCLException();

                case ReturnCode.InvalidBufferSize:
                throw new InvalidBufferSizeOpenCLException();

                case ReturnCode.InvalidMipLevel:
                throw new InvalidMipLevelOpenCLException();

                default:
                throw new UnknownErrorOpenCLException(errorCode);
            }
        }
    }

    public sealed class DeviceNotFoundOpenCLException : OpenCLException { public DeviceNotFoundOpenCLException() : base(ReturnCode.DeviceNotFound) { } }

    public sealed class DeviceNotAvailableOpenCLException : OpenCLException { public DeviceNotAvailableOpenCLException() : base(ReturnCode.DeviceNotAvailable) { } }

    public sealed class CompilerNotAvailableOpenCLException : OpenCLException { public CompilerNotAvailableOpenCLException() : base(ReturnCode.CompilerNotAvailable) { } }

    public sealed class MemoryObjectAllocationFailureOpenCLException : OpenCLException { public MemoryObjectAllocationFailureOpenCLException() : base(ReturnCode.MemoryObjectAllocationFailure) { } }

    public sealed class OutOfResourcesOpenCLException : OpenCLException { public OutOfResourcesOpenCLException() : base(ReturnCode.OutOfResources) { } }

    public sealed class OutOfHostMemoryOpenCLException : OpenCLException { public OutOfHostMemoryOpenCLException() : base(ReturnCode.OutOfHostMemory) { } }

    public sealed class ProfilingInfoNotAvailableOpenCLException : OpenCLException { public ProfilingInfoNotAvailableOpenCLException() : base(ReturnCode.ProfilingInfoNotAvailable) { } }

    public sealed class MemoryCopyOverlapOpenCLException : OpenCLException { public MemoryCopyOverlapOpenCLException() : base(ReturnCode.MemoryCopyOverlap) { } }

    public sealed class ImageFormatMismatchOpenCLException : OpenCLException { public ImageFormatMismatchOpenCLException() : base(ReturnCode.ImageFormatMismatch) { } }

    public sealed class ImageFormatNotSupportedOpenCLException : OpenCLException { public ImageFormatNotSupportedOpenCLException() : base(ReturnCode.ImageFormatNotSupported) { } }

    public sealed class BuildProgramFailureOpenCLException : OpenCLException {
        public String[] BuildLogs { get; private set; }

        public BuildProgramFailureOpenCLException(String[] buildLogs)
            : base(ReturnCode.BuildProgramFailure) {
            BuildLogs = buildLogs;
        }
    }

    public sealed class MapFailureOpenCLException : OpenCLException { public MapFailureOpenCLException() : base(ReturnCode.MapFailure) { } }

    public sealed class InvalidValueOpenCLException : OpenCLException { public InvalidValueOpenCLException() : base(ReturnCode.InvalidValue) { } }

    public sealed class InvalidDeviceTypeOpenCLException : OpenCLException { public InvalidDeviceTypeOpenCLException() : base(ReturnCode.InvalidDeviceType) { } }

    public sealed class InvalidPlatformOpenCLException : OpenCLException { public InvalidPlatformOpenCLException() : base(ReturnCode.InvalidPlatform) { } }

    public sealed class InvalidDeviceOpenCLException : OpenCLException { public InvalidDeviceOpenCLException() : base(ReturnCode.InvalidDevice) { } }

    public sealed class InvalidContextOpenCLException : OpenCLException { public InvalidContextOpenCLException() : base(ReturnCode.InvalidContext) { } }

    public sealed class InvalidCommandQueueFlagsOpenCLException : OpenCLException { public InvalidCommandQueueFlagsOpenCLException() : base(ReturnCode.InvalidCommandQueueFlags) { } }

    public sealed class InvalidCommandQueueOpenCLException : OpenCLException { public InvalidCommandQueueOpenCLException() : base(ReturnCode.InvalidCommandQueue) { } }

    public sealed class InvalidHostPointerOpenCLException : OpenCLException { public InvalidHostPointerOpenCLException() : base(ReturnCode.InvalidHostPointer) { } }

    public sealed class InvalidMemoryObjectOpenCLException : OpenCLException { public InvalidMemoryObjectOpenCLException() : base(ReturnCode.InvalidMemoryObject) { } }

    public sealed class InvalidImageFormatDescriptorOpenCLException : OpenCLException { public InvalidImageFormatDescriptorOpenCLException() : base(ReturnCode.InvalidImageFormatDescriptor) { } }

    public sealed class InvalidImageSizeOpenCLException : OpenCLException { public InvalidImageSizeOpenCLException() : base(ReturnCode.InvalidImageSize) { } }

    public sealed class InvalidSamplerOpenCLException : OpenCLException { public InvalidSamplerOpenCLException() : base(ReturnCode.InvalidSampler) { } }

    public sealed class InvalidBinaryOpenCLException : OpenCLException { public InvalidBinaryOpenCLException() : base(ReturnCode.InvalidBinary) { } }

    public sealed class InvalidBuildOptionsOpenCLException : OpenCLException { public InvalidBuildOptionsOpenCLException() : base(ReturnCode.InvalidBuildOptions) { } }

    public sealed class InvalidProgramOpenCLException : OpenCLException { public InvalidProgramOpenCLException() : base(ReturnCode.InvalidProgram) { } }

    public sealed class InvalidProgramExecutableOpenCLException : OpenCLException { public InvalidProgramExecutableOpenCLException() : base(ReturnCode.InvalidProgramExecutable) { } }

    public sealed class InvalidKernelNameOpenCLException : OpenCLException { public InvalidKernelNameOpenCLException() : base(ReturnCode.InvalidKernelName) { } }

    public sealed class InvalidKernelDefinitionOpenCLException : OpenCLException { public InvalidKernelDefinitionOpenCLException() : base(ReturnCode.InvalidKernelDefinition) { } }

    public sealed class InvalidKernelOpenCLException : OpenCLException { public InvalidKernelOpenCLException() : base(ReturnCode.InvalidKernel) { } }

    public sealed class InvalidArgumentIndexOpenCLException : OpenCLException { public InvalidArgumentIndexOpenCLException() : base(ReturnCode.InvalidArgumentIndex) { } }

    public sealed class InvalidArgumentValueOpenCLException : OpenCLException { public InvalidArgumentValueOpenCLException() : base(ReturnCode.InvalidArgumentValue) { } }

    public sealed class InvalidArgumentSizeOpenCLException : OpenCLException { public InvalidArgumentSizeOpenCLException() : base(ReturnCode.InvalidArgumentSize) { } }

    public sealed class InvalidKernelArgumentsOpenCLException : OpenCLException { public InvalidKernelArgumentsOpenCLException() : base(ReturnCode.InvalidKernelArguments) { } }

    public sealed class InvalidWorkDimensionsOpenCLException : OpenCLException { public InvalidWorkDimensionsOpenCLException() : base(ReturnCode.InvalidWorkDimension) { } }

    public sealed class InvalidWorkGroupSizeOpenCLException : OpenCLException { public InvalidWorkGroupSizeOpenCLException() : base(ReturnCode.InvalidWorkGroupSize) { } }

    public sealed class InvalidWorkItemSizeOpenCLException : OpenCLException { public InvalidWorkItemSizeOpenCLException() : base(ReturnCode.InvalidWorkItemSize) { } }

    public sealed class InvalidGlobalOffsetOpenCLException : OpenCLException { public InvalidGlobalOffsetOpenCLException() : base(ReturnCode.InvalidGlobalOffset) { } }

    public sealed class InvalidEventWaitListOpenCLException : OpenCLException { public InvalidEventWaitListOpenCLException() : base(ReturnCode.InvalidEventWaitList) { } }

    public sealed class InvalidEventOpenCLException : OpenCLException { public InvalidEventOpenCLException() : base(ReturnCode.InvalidEvent) { } }

    public sealed class InvalidOperationOpenCLException : OpenCLException { public InvalidOperationOpenCLException() : base(ReturnCode.InvalidOperation) { } }

    public sealed class InvalidGLObjectOpenCLException : OpenCLException { public InvalidGLObjectOpenCLException() : base(ReturnCode.InvalidGLObject) { } }

    public sealed class InvalidBufferSizeOpenCLException : OpenCLException { public InvalidBufferSizeOpenCLException() : base(ReturnCode.InvalidBufferSize) { } }

    public sealed class InvalidMipLevelOpenCLException : OpenCLException { public InvalidMipLevelOpenCLException() : base(ReturnCode.InvalidMipLevel) { } }

    public sealed class UnknownErrorOpenCLException : OpenCLException { internal UnknownErrorOpenCLException(ReturnCode code) : base(code) { } }
}
