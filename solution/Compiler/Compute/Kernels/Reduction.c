// NOTE: not used at the time

#pragma OPENCL EXTENSION cl_khr_fp64 : enable

#define Reduction(a, b) {reduction}

kernel void Kernel(
		ulong n,
		local {type} * communication{arguments}
) {
	int globalIndex = get_global_id(0);
	int localIndex = get_local_id(0);
	int globalSize = get_global_size(0);
	int groupSize = get_local_size(0);
	int groupIndex = get_group_id(0);
	int index = globalIndex;
	{type} value = {identity};
	while (index < n) {
		#define Access(argument) argument[index]
		value = Reduction(value, {expression});
		#undef Access
		index += globalSize;
	}
	int mask = groupSize >> 1; // assuming group size is power of 2
	while (mask > 0) {
		if ((localIndex & mask) == mask) {
			communication[localIndex] = value;
			barrier(CLK_LOCAL_MEM_FENCE);
			return;
		} else {
			barrier(CLK_LOCAL_MEM_FENCE);
			value = Reduction(value, communication[localIndex ^ mask]);
		}
		mask = mask >> 1;
	}
	#define Access(argument) argument[0]
	{target} = {resultExpression};
	#undef Access
}