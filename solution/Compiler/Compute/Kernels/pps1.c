/* pps {operation}
 * 1st stage
 * taken from http://developer.amd.com
 */
kernel void Kernel(
		const ulong length,
		local {type} * scratch{arguments}
	)
{
	ulong global_index = get_global_id(0);
	{type} acc = {identity};
	while (global_index < length) {
#define Access(memory) memory[global_index]
		{type} element = {expression};
#undef Access
#define mine acc
#define other element
		acc = {operation};
#undef other
#undef mine
		global_index += get_global_size(0);
	}

	ulong local_index = get_local_id(0);
	scratch[local_index] = acc;
	barrier(CLK_LOCAL_MEM_FENCE);

	for(ulong offset = get_local_size(0)>>1; offset > 0; offset>>=1) {
		if (local_index < offset) {
			{type} other = scratch[local_index + offset];
			{type} mine = scratch[local_index];
			scratch[local_index] = {operation};
		}
	}

	if (local_index == 0) {
#define Access(memory) memory[get_group_id(0)]
		{target} = scratch[0];
#undef Access
	}
}
