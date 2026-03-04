document.addEventListener('alpine:init', () => {
    Alpine.data('layout', () => ({
        sidebarOpen: false, 
        darkMode: localStorage.getItem('theme') === 'dark',
        init() {
            // Inicializar tema
            if (this.darkMode) document.documentElement.classList.add('dark');
            else document.documentElement.classList.remove('dark');
        },
        toggleTheme() {
            this.darkMode = !this.darkMode;
            localStorage.setItem('theme', this.darkMode ? 'dark' : 'light');
            if (this.darkMode) document.documentElement.classList.add('dark');
            else document.documentElement.classList.remove('dark');
        }
    }));

    Alpine.data('imageCaptureApp', () => ({
        isCameraOpen: false,
        stream: null,
        isFrontCamera: true, 
        hasMultipleCameras: false,

        init() {

            const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
            this.isFrontCamera = !isMobile;

            if (navigator.mediaDevices && navigator.mediaDevices.enumerateDevices) {
                navigator.mediaDevices.enumerateDevices().then(devices => {
                    const videoInputs = devices.filter(device => device.kind === 'videoinput');
                    this.hasMultipleCameras = videoInputs.length > 1;
                });
            }
        },

        openCamera() {
            this.isCameraOpen = true;
            this.startStream();
        },

        closeCamera() {
            this.isCameraOpen = false;
            this.stopStream();
        },

        async startStream() {
            this.stopStream(); 
            try {
                this.stream = await navigator.mediaDevices.getUserMedia({
                    video: { facingMode: this.isFrontCamera ? 'user' : 'environment' },
                    audio: false
                });
                this.$refs.videoElement.srcObject = this.stream;

                navigator.mediaDevices.enumerateDevices().then(devices => {
                    const videoInputs = devices.filter(device => device.kind === 'videoinput');
                    this.hasMultipleCameras = videoInputs.length > 1;
                });
            } catch (err) {
                console.error("Error al acceder a la cámara: ", err);
                alert("No se pudo acceder a la cámara. Por favor revisa los permisos del navegador.");
                this.closeCamera();
            }
        },

        stopStream() {
            if (this.stream) {
                this.stream.getTracks().forEach(track => track.stop());
                this.stream = null;
            }
        },

        switchCamera() {
            this.isFrontCamera = !this.isFrontCamera;
            this.startStream();
        },

        takePhoto() {
            const video = this.$refs.videoElement;
            const canvas = document.createElement('canvas');

            // Ajustar el canvas a la resolución real del video
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;

            const ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

            // Convertir a Base64 con calidad del 80% (Para no saturar tu servidor)
            const base64Image = canvas.toDataURL('image/jpeg', 0.8);

            // LA MAGIA: Despachamos un evento global con la foto
            this.$dispatch('photo-captured', base64Image);

            this.closeCamera();
        },

        handleFileUpload(event) {
            const file = event.target.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = (e) => {
                // Despachamos el mismo evento si suben un archivo
                this.$dispatch('photo-captured', e.target.result);
                this.closeCamera();
            };
            reader.readAsDataURL(file);
        }
    }));
});