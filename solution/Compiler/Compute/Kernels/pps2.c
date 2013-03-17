/* pps {operation}
 * 2st stage
 */
kernel void Kernel(
		const ulong length{arguments}
	)
{
	{type} value = {identity};
	for (ulong i = 0; i < length; i++) {
#define Access(memory) memory[i]
		{type} other = {expression};
#undef Access
#define mine value
		value = {operation};
#undef mine
	}
#define Access(memory) memory[0]
	{target} = {resultExpression};
#undef Access
}