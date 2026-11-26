<template>
	<img
		v-if="club.brand.logoDataUrl"
		:src="club.brand.logoDataUrl"
		:alt="club.brand.clubName"
		:style="{ height }"
		class="d-block" />
	<span v-else class="voxa-heading text-primary" :style="{ fontSize: '1.1rem' }">{{ club.brand.clubName }}</span>
</template>

<script setup lang="ts">
import { useClubStore } from "@/store/ClubStore"

withDefaults(defineProps<{ height?: string }>(), { height: "44px" })

const club = useClubStore()
</script>
