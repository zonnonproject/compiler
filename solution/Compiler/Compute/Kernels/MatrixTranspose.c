/* Matrix Transpose
 * global range n x m
 * local range null
 * no local memory */
kernel void Kernel(
		ulong n, 
		ulong m{arguments}
	)
{
#define Access(matrix) matrix[get_global_id(0)*n+get_global_id(1)]
	{type} value = {leftExpression};
#undef Access
#define Access(matrix) matrix[get_global_id(1)*m+get_global_id(0)]
	{target} = {resultExpression};
#undef Access
}
