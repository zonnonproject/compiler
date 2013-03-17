/* Matrix Exponent
 * global range 'smallest a where a=16*k > n' x 'smallest b where b=16*l > n'
 * local range 16 x 16
 * local memory 16*16*float
 */
kernel void Kernel(
		ulong n,
		local {type} * A,
		local {type} * B{arguments}
	)
{
	ulong m = n;
	ulong p = n;
	ulong4 pos = (ulong4)(get_global_id(0), get_global_id(1), get_local_id(0), get_local_id(1));
	for (ulong pow = 1; pow < {power}; pow++) {
		{type} value = 0.0f;

		for (ulong i = 0; i < m; i += 16) {
#define Access(matrix) (matrix)[pos.y*m + pos.z + i]
			A[(pos.w<<4) + pos.z] = {leftExpression};
#undef Access
			if (pos.y >= m || pos.z+i >= n) A[(pos.w<<4) + pos.z] = 0;
#define Access(matrix) (matrix)[(pos.w+i)*p + pos.x]
			B[(pos.w<<4) + pos.z] = {leftExpression};
#undef Access
			if (pos.w+i >= m || pos.x >= p) B[(pos.w<<4) + pos.z] = 0;
			barrier(CLK_LOCAL_MEM_FENCE);

			{type}16 a = vload16(pos.w, A);
			value += a.s0 * B[          pos.z];
			value += a.s1 * B[( 1<<4) + pos.z];
			value += a.s2 * B[( 2<<4) + pos.z];
			value += a.s3 * B[( 3<<4) + pos.z];
			value += a.s4 * B[( 4<<4) + pos.z];
			value += a.s5 * B[( 5<<4) + pos.z];
			value += a.s6 * B[( 6<<4) + pos.z];
			value += a.s7 * B[( 7<<4) + pos.z];
			value += a.s8 * B[( 8<<4) + pos.z];
			value += a.s9 * B[( 9<<4) + pos.z];
			value += a.sa * B[(10<<4) + pos.z];
			value += a.sb * B[(11<<4) + pos.z];
			value += a.sc * B[(12<<4) + pos.z];
			value += a.sd * B[(13<<4) + pos.z];
			value += a.se * B[(14<<4) + pos.z];
			value += a.sf * B[(15<<4) + pos.z];
			barrier(CLK_LOCAL_MEM_FENCE);
		}
#define Access(matrix) (matrix)[pos.y*p + pos.x]
		if (pos.x < n && pos.y < p)
			{target} = {resultExpression};
#undef Access
		barrier(CLK_GLOBAL_MEM_FENCE);
	}
}