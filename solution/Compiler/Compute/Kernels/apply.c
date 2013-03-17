/* apply {operation}
 * apply operation to all elements
 */
kernel void Kernel(
		const ulong length{arguments}
	)
{
	ulong id = get_global_id(0);

#define Access(memory) memory[id]
	{type} value = {expression};
#undef Access
	value = {operation};
#define Access(memory) memory[id]
	{target} = {resultExpression};
#undef Access
}