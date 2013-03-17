/* all {expression}
 * check if all elements
 * result:
 *		0 - true for all elements
 *		1 - false for some elements
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
	if (!value)
		{target} = !value;
#undef Access
}