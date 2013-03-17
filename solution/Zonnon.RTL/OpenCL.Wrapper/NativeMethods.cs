//-----------------------------------------------------------------------------
//
//  Copyright (c) 2000-2013 ETH Zurich (http://www.ethz.ch) and others.
//  All rights reserved. This program and the accompanying materials
//  are made available under the terms of the Microsoft Public License.
//  which accompanies this distribution, and is available at
//  http://opensource.org/licenses/MS-PL
//
//  Contributors:
//    ETH Zurich, Native Systems Group - Initial contribution and API
//    http://zonnon.ethz.ch/contributors.html
//
//-----------------------------------------------------------------------------

namespace Zonnon.RTL.OpenCL.Wrapper {
    using System;
    using System.Runtime.InteropServices;

    static class NativeMethods {
        const string openCL = "opencl.dll";

        public delegate void EventNotificationCallback(IntPtr handle, CommandExecutionStatus event_command_exec_status, IntPtr user_data);

        // see OpenCL specification for details about the method calls and parameters

        [DllImport(openCL, EntryPoint = "clGetPlatformIDs")]
        public extern static ReturnCode GetPlatformIDs(
            Int32 num_entries,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] platforms,
            out Int32 num_platforms
        );

        [DllImport(openCL, EntryPoint = "clGetDeviceIDs")]
        public extern static ReturnCode GetDeviceIDs(
            IntPtr platform,
            DeviceType device_type, Int32 num_entries,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] devices,
            out Int32 num_devices
        );

        [DllImport(openCL, EntryPoint = "clGetDeviceInfo")]
        public static extern ReturnCode GetDeviceInfo(
            IntPtr device_id,
            DeviceInfo param_name,
            IntPtr param_value_size,
            IntPtr param_value,
            IntPtr param_value_size_ret
        );

        public delegate void ContextNotificationCallback(
            [MarshalAs(UnmanagedType.LPStr)] String errinfo,
            IntPtr private_info,
            IntPtr cb,
            IntPtr user_data
        );

        [DllImport(openCL, EntryPoint = "clCreateContext")]
        public static extern IntPtr CreateContext(
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] properties,
            Int32 num_devices,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] devices,
            [MarshalAs(UnmanagedType.FunctionPtr)] ContextNotificationCallback pfn_notify,
            IntPtr user_data,
            out ReturnCode errcode_ret
        );

        [DllImport(openCL, EntryPoint = "clReleaseContext")]
        public static extern ReturnCode ReleaseContext(IntPtr context);

        [DllImport(openCL, EntryPoint = "clCreateCommandQueue")]
        public static extern IntPtr CreateCommandQueue(
            IntPtr context,
            IntPtr device,
            CommandQueueFlags
            properties,
            out ReturnCode errcode_ret
        );

        [DllImport(openCL, EntryPoint = "clFlush")]
        public static extern ReturnCode Flush(IntPtr command_queue);

        [DllImport(openCL, EntryPoint = "clReleaseCommandQueue")]
        public static extern ReturnCode ReleaseCommandQueue(
            IntPtr queue
        );

        [DllImport(openCL, EntryPoint = "clCreateBuffer")]
        public static extern IntPtr CreateBuffer(
            IntPtr context,
            BufferFlags flags,
            UIntPtr size,
            IntPtr host_ptr,
            out ReturnCode errcode_ret
        );

        [DllImport(openCL, EntryPoint = "clReleaseMemObject")]
        public static extern ReturnCode ReleaseMemoryObject(
            IntPtr memobj
        );

        [DllImport(openCL, EntryPoint = "clEnqueueReadBuffer")]
        public static extern ReturnCode ReadBuffer(
            IntPtr queue,
            IntPtr buffer,
            OpenCLBoolean blocking_read,
            IntPtr offset,
            IntPtr cb,
            IntPtr ptr,
            Int32 num_events_in_wait_list,
            IntPtr event_wait_list,
            IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clEnqueueWriteBuffer")]
        public static extern ReturnCode WriteBuffer(
            IntPtr queue,
            IntPtr buffer,
            OpenCLBoolean blocking_write,
            IntPtr offset,
            IntPtr cb,
            IntPtr ptr,
            Int32 num_events_in_wait_list,
            IntPtr event_wait_list,
            IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clEnqueueReadBuffer")]
        public static extern ReturnCode BeginReadBuffer(
            IntPtr queue,
            IntPtr buffer,
            OpenCLBoolean blocking_read,
            IntPtr offset,
            IntPtr cb,
            IntPtr ptr,
            Int32 num_events_in_wait_list,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] event_wait_list,
            out IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clEnqueueWriteBuffer")]
        public static extern ReturnCode BeginWriteBuffer(
            IntPtr queue,
            IntPtr buffer,
            OpenCLBoolean blocking_write,
            IntPtr offset,
            IntPtr cb,
            IntPtr ptr,
            Int32 num_events_in_wait_list,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] event_wait_list,
            out IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clEnqueueReadBufferRect")]
        public static extern ReturnCode BeginReadBufferRect(
            IntPtr queue,
            IntPtr buffer,
            OpenCLBoolean blocking_read,
            IntPtr[] buffer_origin,
            IntPtr[] host_origin,
            IntPtr[] region,
            IntPtr buffer_row_pitch,
            IntPtr buffer_slice_pitch,
            IntPtr host_row_pitch,
            IntPtr host_slice_pitch,
            IntPtr ptr,
            Int32 num_events_in_wait_list,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] event_wait_list,
            out IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clEnqueueWriteBufferRect")]
        public static extern ReturnCode BeginWriteBufferRect(
            IntPtr queue,
            IntPtr buffer,
            OpenCLBoolean blocking_write,
            IntPtr[] buffer_origin,
            IntPtr[] host_origin,
            IntPtr[] region,
            IntPtr buffer_row_pitch,
            IntPtr buffer_slice_pitch,
            IntPtr host_row_pitch,
            IntPtr host_slice_pitch,
            IntPtr ptr,
            Int32 num_events_in_wait_list,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] event_wait_list,
            out IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clEnqueueCopyBuffer")]
        public static extern ReturnCode CopyBuffer(
            IntPtr queue,
            IntPtr src_buffer,
            IntPtr dst_buffer,
            IntPtr src_offset,
            IntPtr dst_offset,
            IntPtr cb,
            Int32 num_events_in_wait_list,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] event_wait_list,
            out IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clCreateProgramWithSource")]
        public static extern IntPtr CreateProgramWithSource(
            IntPtr context, 
            Int32 count, 
            String[] source, 
            IntPtr[] lengths, 
            out ReturnCode errcode_ret
        );

        [DllImport(openCL, EntryPoint = "clBuildProgram")]
        public static extern ReturnCode BuildProgram(
            IntPtr program,
            Int32 num_devices,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] device_list,
            String options,
            IntPtr pfn_notify,
            IntPtr user_data
        );

        [DllImport(openCL, EntryPoint = "clReleaseProgram")]
        public static extern ReturnCode ReleaseProgram(IntPtr program);

        [DllImport(openCL, EntryPoint = "clGetProgramBuildInfo")]
        public static extern ReturnCode GetProgramBuildInfoString(
            IntPtr program,
            IntPtr device,
            ProgramBuildInfo param_name,
            IntPtr param_value_size,
            IntPtr param_value,
            out IntPtr param_value_size_ret
        );

        [DllImport(openCL, EntryPoint = "clCreateKernel")]
        public static extern IntPtr CreateKernel(
            IntPtr program,
            String kernel_name,
            out ReturnCode errcode_ret
        );

        [DllImport(openCL, EntryPoint = "clReleaseKernel")]
        public static extern ReturnCode ReleaseKernel(IntPtr kernel);

        [DllImport(openCL, EntryPoint = "clSetKernelArg")]
        public static extern ReturnCode SetKernelArgument(
            IntPtr kernel,
            Int32 arg_index,
            IntPtr arg_size,
            IntPtr arg_value
        );

        [DllImport(openCL, EntryPoint = "clEnqueueNDRangeKernel")]
        public static extern ReturnCode EnqueueNDRangeKernel(
            IntPtr queue,
            IntPtr kernel,
            Int32 work_dim,
            [MarshalAs(UnmanagedType.LPArray)] UIntPtr[] global_work_offset,
            [MarshalAs(UnmanagedType.LPArray)] UIntPtr[] global_work_size,
            [MarshalAs(UnmanagedType.LPArray)] UIntPtr[] local_work_size,
            Int32 num_events_in_wait_list,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] event_wait_list,
            out IntPtr new_event
        );

        [DllImport(openCL, EntryPoint = "clFinish")]
        public static extern ReturnCode Finish(IntPtr queue);

        [DllImport(openCL, EntryPoint = "clGetEventInfo")]
        public static extern ReturnCode GetEventInfo(
            IntPtr handle,
            EventInfo param_name,
            IntPtr param_value_size,
            IntPtr param_value,
            IntPtr param_value_size_ret
        );

        [DllImport(openCL, EntryPoint = "clGetEventProfilingInfo")]
        public static extern ReturnCode GetEventProfilingInfo(IntPtr handle, CommandProfilingInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr param_value_size_ret);

        [DllImport(openCL, EntryPoint = "clSetEventCallback")]
        public static extern ReturnCode SetEventCallback(IntPtr handle, CommandExecutionStatus command_exec_callback_type, EventNotificationCallback pfn_event_notify, IntPtr user_data);

        [DllImport(openCL, EntryPoint = "clReleaseEvent")]
        public static extern ReturnCode ReleaseEvent(IntPtr handle);

        [DllImport(openCL, EntryPoint = "clWaitForEvents")]
        public static extern ReturnCode WaitForEvents(
            Int32 num_events,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] event_list
        );
    }
}
