/* Left LU Division
 */
kernel void Kernel(
		const ulong length,
		global {type} * z,
		global {type} * L/* not a mistake! */
		/* first arg is U */{arguments}
	)
{
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
#define Access(memory) memory[i*length+p]
			{type} temp = {expression};
#undef Access
#define Access(memory) memory[p]
			sum += temp * {target};
#undef Access
		}
#define Access(memory) memory[i*length+i]
		{type} temp = {expression};
#undef Access
#define Access(memory) memory[i]
		{target} = (z[i] - sum) / temp;
#undef Access
	}
}