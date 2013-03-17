/* Vector Matrix Multiplication
 * global range p
 * local range null
 * no local memory
 */
kernel void Kernel(
		ulong m,
		ulong p{arguments}
	)
{
	{type} value = 0;
	for(ulong row = 0; row < m; row += 1) {
		#define Access(argument) argument[row]
		{type} left = {leftExpression};
		#undef Access
		#define Access(argument) argument[row * p + get_local_id(0)]
		{type} right = {rightExpression};
		#undef Access
		value += left * right;
	}
	#define Access(argument) argument[get_local_id(0)]
	{target} = {resultExpression};
	#undef Access
}
