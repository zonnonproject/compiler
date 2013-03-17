/* Left Division
 */
kernel void Kernel(
		const ulong length,
		global {type} * L,
		global {type} * U,
		global {type} * z{arguments}
	)
{
	for (long k = 0; k < length; k++) {
		U[k*length+k] = 1;

		for (long i = k; i < length; i++) {
			{type} sum = 0;
			for (long p = 0; p < k; p++) {
				sum += L[i*length+p] * U[p*length+k];
			}
#define Access(memory) memory[i*length+k]
			L[i*length+k] = {leftExpression} - sum;
#undef Access
		}

		for (long j = k; j < length; j++) {
			{type} sum = 0;
			for (long p = 0; p < k; p++) {
				sum += L[k*length+p] * U[p*length+j];
			}
#define Access(memory) memory[k*length+j]
			U[k*length+j] = ({leftExpression} - sum) / L[k*length+k];
#undef Access
		}
	}
	
	for (long i = 0; i < length; i++) {
		{type} sum = 0;
		for (long p = 0; p < i; p++) {
			sum += L[i*length+p] * z[p];
		}
#define Access(memory) memory[i]
		z[i] = ({rightExpression} - sum) / L[i*length+i];
#undef Access
	}

	for (long i = length-1; i >= 0; i--) {
		{type} sum = 0;
		for (long p = length-1; p > i; p--) {
#define Access(memory) memory[p]
			sum += U[i*length+p] * {target};
#undef Access
		}
#define Access(memory) memory[i]
		{target} = (z[i] - sum) / U[i*length+i];
#undef Access
	}
}