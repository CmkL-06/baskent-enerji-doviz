import { ref } from 'vue'

/**
 * Tek bir "açık" satır/kart kimliği tutan genel açılır-kart durumu.
 * Aynı anda yalnızca bir öğe açık kalır (akordeon davranışı).
 */
export function useExpandable<T = string>() {
  const expandedId = ref<T | null>(null)

  function toggle(id: T) {
    expandedId.value = expandedId.value === id ? null : id
  }

  function isExpanded(id: T) {
    return expandedId.value === id
  }

  function close() {
    expandedId.value = null
  }

  return { expandedId, toggle, isExpanded, close }
}
