#ifndef OWNPRNG_PRNG_CONSTANTS_H
#define OWNPRNG_PRNG_CONSTANTS_H

#include <cstddef>
#include <cstdint>
#include <cmath>

namespace PRNG {
  const size_t BLOCK_SIZE = 32;
  const uint64_t MASK_U64_F64 = 0x1fffffffffffffull;
  const double MIN_VAL_F64 = pow(2., -53.);
}

#endif //OWNPRNG_PRNG_CONSTANTS_H
