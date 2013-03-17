/* element wise copy
 * {target} = {resultExpression}
 */
kernel void Kernel(
		ulong count{arguments}
	)
{
	ulong index = get_local_id(0);

#define Access(arr) arr[index] 
	{target} = {resultExpression};
#undef Access
}
