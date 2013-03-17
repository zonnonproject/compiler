namespace ETH.Zonnon.Compute {
    using System;
    using System.Compiler;

    sealed class ComputeScalarType {
        public static ComputeScalarType Single = new ComputeScalarType("float", 4, SystemTypes.Single);

        public String OpenCLTypeName { get; private set;}
        public Int32 ByteSize { get; private set; }
        public TypeNode TypeNode { get; private set; }

        private ComputeScalarType(String openCLTypeName, Int32 byteSize, TypeNode typeNode) {
            OpenCLTypeName = openCLTypeName;
            ByteSize = byteSize;
            TypeNode = typeNode;
        }

        public static Boolean TryGet(TYPE type, out ComputeScalarType computeType) {
            REAL_TYPE realType;
            if (type.Is(out realType)) {
                if (realType.width == 32) {
                    computeType = Single;
                    return true;
                }
            }
            /*CARDINAL_TYPE cardType;
            if (type.Is(out cardType)) {
                if (cardType.width == 32) {
                    computeType = Single;
                    return true;
                }
            }*/
            computeType = null;
            return false;
        }
    }
}