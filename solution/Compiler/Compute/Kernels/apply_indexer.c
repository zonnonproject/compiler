/* apply {operation}
 * apply operation to all elements
 */
kernel void Kernel(
		const ulong length{arguments}
	)
{
	ulong row = get_global_id(1);
	ulong col = get_global_id(0);

#define Access(mem,n,f0,b0,f1,b1) (mem)[((b0)*row+(f0))*(n)+((b1)*(col)+(f1))]
	{type} value = {expression};
#undef Access
	value = {operation};
#define Access(mem,n,f0,b0,f1,b1) (mem)[((b0)*row+(f0))*(n)+((b1)*(col)+(f1))]
	{target} = {resultExpression};
#undef Access
}