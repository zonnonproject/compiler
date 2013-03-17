/* LU Decomposition
 */
kernel void Kernel(
		const ulong length,
		global {type} * L{arguments}
		/* last arg is U */
	)
{
	for (long k = 0; k < length; k++) {
#define Access(memory) memory[k*length+k]
		{target} = 1;
#undef Access

		for (long i = k; i < length; i++) {
			{type} sum = 0;
			for (long p = 0; p < k; p++) {
#define Access(memory) memory[p*length+k]
				sum += L[i*length+p] * {target};
#undef Access
			}
#define Access(memory) memory[i*length+k]
			L[i*length+k] = {expression} - sum;
#undef Access
		}

		for (long j = k; j < length; j++) {
			{type} sum = 0;
			for (long p = 0; p < k; p++) {
#define Access(memory) memory[p*length+j]
				sum += L[k*length+p] * {target};
#undef Access
			}
#define Access(memory) memory[k*length+j]
			{target} = ({expression} - sum) / L[k*length+k];
#undef Access
		}
	}
}