/* Matrix Vector Multiplication
 * global range 32 x N
 * local range 32 x 1
 * local memory 32*float
 */
kernel void Kernel(
		ulong n,
		ulong m,
		local {type} * work{arguments}
	)
{
	{type}16 s = ({type}16)(0);
	ulong mmax = m>>4;
	ulong mrest = m&0xf;
	for (ulong i = get_global_id(0); i < mmax; i += get_global_size(0)) {
#define Access(argument) vload16(i, argument + get_global_id(1) * m)
		{type}16 left = {leftExpression};
#undef Access
#define Access(argument) vload16(i, argument)
		{type}16 right = {rightExpression};
#undef Access
		s += left*right;
	}

	{type} sum1 = s.s0 + s.s1 + s.s2 + s.s3;
	{type} sum2 = s.s4 + s.s5 + s.s6 + s.s7;
	{type} sum3 = s.s8 + s.s9 + s.sa + s.sb;
	{type} sum4 = s.sc + s.sd + s.se + s.sf;
	{type} sum = sum1 + sum2 + sum3 + sum4;
	for (ulong i = m-mrest+get_global_id(0); i < m; i += get_global_size(0)) {
#define Access(argument) argument[get_global_id(1) * m + i]
		{type} left = {leftExpression};
#undef Access
#define Access(argument) argument[i]
		{type} right = {rightExpression};
#undef Access
		sum += left*right;
	}

	ulong li = get_local_id(0);
	work[li] = sum;
	barrier(CLK_LOCAL_MEM_FENCE);

	ulong cols = get_local_size(0);
	while (cols > 1) {
		cols >>= 1;
		if (li < cols) work[li] += work[cols+li];
		barrier(CLK_LOCAL_MEM_FENCE);
	}
	if (li == 0) {
		{type} value = work[li];
#define Access(argument) argument[get_global_id(1)]
		{target} = {resultExpression};
#undef Access
	}
}
