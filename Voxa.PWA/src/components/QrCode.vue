<template>
	<img
		v-if="src"
		:src="src"
		:alt="$t('board.live.qrAlt', { url: value })"
		:width="size"
		:height="size"
		class="d-block" />
</template>

<script setup lang="ts">
import QRCode from "qrcode"
import { ref, watchEffect } from "vue"

const props = withDefaults(defineProps<{ value: string; size?: number; dark?: string }>(), {
	size: 200,
	dark: "#000000",
})
const src = ref("")

// A data URL, so the Content-Security-Policy (img-src 'self' data:) allows it without another host.
watchEffect(() => {
	void QRCode.toDataURL(props.value, {
		width: props.size * 2,
		margin: 1,
		color: { dark: props.dark, light: "#FFFFFF" },
	}).then((url) => (src.value = url))
})
</script>
