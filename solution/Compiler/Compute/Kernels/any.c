/* any {expression}
 * check if any element ...
 * result:
 *		1 - true for any element
 *		0 - false for all elements
 */
kernel void Kernel(
		const ulong length{arguments}
	)
{
	ulong id = get_global_id(0);

#define Access(memory) memory[id]
	bool value = {expression};
#undef Access
#define Access(memory) memory[0]
	if (value)
		{target} = value;
#undef Access
}