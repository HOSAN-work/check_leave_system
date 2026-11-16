<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'

const now = ref(Date.now())
let clockInterval

const timeParts = computed(() => {
  const date = new Date(now.value)
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')

  return { hours, minutes }
})

const digitalTime = computed(() => `${timeParts.value.hours}:${timeParts.value.minutes}`)
const timeLabel = computed(() => `${timeParts.value.hours} 點 ${timeParts.value.minutes} 分`)
const timeDigits = computed(() => `${timeParts.value.hours}${timeParts.value.minutes}`.split(''))

const digitSegments = [
  { id: 'top', path: 'M0.772551 0H80.3459L65.6673 13H14.6786L0.772551 0Z' },
  {
    id: 'middle',
    path: 'M12.3609 71.5H67.985L78.8008 78L67.985 84.5H12.3609L2.31768 78L12.3609 71.5Z',
  },
  { id: 'upperLeft', path: 'M0 76.375V3.25L12.3609 15.4375V68.25L0 76.375Z' },
  { id: 'upperRight', path: 'M80.3459 4.06249V76.375L67.985 68.25V15.4375L80.3459 4.06249Z' },
  { id: 'bottom', path: 'M77.2556 156H3.09023L14.6786 143H65.6673L77.2556 156Z' },
  { id: 'lowerRight', path: 'M80.3459 80.4375V154.375L67.985 141.375V87.75L80.3459 80.4375Z' },
  { id: 'lowerLeft', path: 'M0 154.375V79.625L12.3609 87.75V140.563L0 154.375Z' },
]

const digitPatterns = {
  0: ['top', 'upperLeft', 'upperRight', 'lowerLeft', 'lowerRight', 'bottom'],
  1: ['upperRight', 'lowerRight'],
  2: ['top', 'upperRight', 'middle', 'lowerLeft', 'bottom'],
  3: ['top', 'upperRight', 'middle', 'lowerRight', 'bottom'],
  4: ['upperLeft', 'upperRight', 'middle', 'lowerRight'],
  5: ['top', 'upperLeft', 'middle', 'lowerRight', 'bottom'],
  6: ['top', 'upperLeft', 'middle', 'lowerLeft', 'lowerRight', 'bottom'],
  7: ['top', 'upperRight', 'lowerRight'],
  8: ['top', 'upperLeft', 'upperRight', 'middle', 'lowerLeft', 'lowerRight', 'bottom'],
  9: ['top', 'upperLeft', 'upperRight', 'middle', 'lowerRight', 'bottom'],
}

onMounted(() => {
  clockInterval = window.setInterval(() => {
    now.value = Date.now()
  }, 1000)
})

onUnmounted(() => {
  window.clearInterval(clockInterval)
})
</script>

<template>
  <time class="clock-display" :datetime="digitalTime" :aria-label="timeLabel">
    <template v-for="(digit, index) in timeDigits" :key="`digit-${index}`">
      <svg class="clock-digit" viewBox="0 0 80.3459 156" aria-hidden="true" focusable="false">
        <path
          v-for="segment in digitSegments"
          :key="segment.id"
          :d="segment.path"
          :fill="digitPatterns[digit].includes(segment.id) ? '#171717' : '#E6E6E6'"
        />
      </svg>
      <span v-if="index === 1" class="clock-colon" aria-hidden="true"><i></i><i></i></span>
    </template>
  </time>
</template>

<style scoped>
.clock-display {
  display: flex;
  align-items: center;
  gap: clamp(8px, 1.22vw, 18.5px);
  white-space: nowrap;
}

.clock-digit {
  display: block;
  width: clamp(48px, 5.315vw, 80.35px);
  height: clamp(93px, 10.32vw, 156px);
  flex: 0 1 auto;
}

.clock-colon {
  display: flex;
  flex: 0 0 clamp(10px, 1.02vw, 15.45px);
  flex-direction: column;
  justify-content: center;
  gap: clamp(9px, 1.93vw, 29.25px);
}

.clock-colon i {
  display: block;
  width: 100%;
  aspect-ratio: 1;
  background: #171717;
}

@media (max-width: 680px) {
  .clock-display {
    gap: 8px;
  }

  .clock-digit {
    width: clamp(42px, 12vw, 62px);
    height: clamp(82px, 23.3vw, 120px);
  }

  .clock-colon {
    flex-basis: 12px;
    gap: 18px;
  }
}

/* 冒號一閃一閃動畫 */
.clock-colon {
  /* 保留原本的 display、gap 等設定 */
  animation: colon-blink 1s steps(1, end) infinite;
}
@keyframes colon-blink {
  50% {
    opacity: 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .clock-colon {
    animation: none;
  }
}
</style>
