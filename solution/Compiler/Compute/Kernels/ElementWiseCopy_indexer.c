/* indexed element wise copy
 * {target} = {resultExpression}
 */
kernel void Kernel(
		ulong count{arguments}
	)
{
	ulong row = get_global_id(1);
	ulong col = get_global_id(0);

#define Access(mem,n,f0,b0,f1,b1) (mem)[((b0)*row+(f0))*(n)+((b1)*(col)+(f1))]
	{target} = {resultExpression};
#undef Access
}
