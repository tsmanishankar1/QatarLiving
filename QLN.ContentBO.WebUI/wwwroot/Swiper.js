         let mainSwiperInstance;
         let thumbsSwiperInstance;

         window.initializeSwiperGallery = (dotNetRef, uniqueId) => {
             const container = document.getElementById(uniqueId);
             if (!container) {
                 console.warn("Swiper container not found:", uniqueId);
                 return;
             }

             // Destroy previous Swiper instances if they exist
             if (mainSwiperInstance) {
                 mainSwiperInstance.destroy(true, true);
                 mainSwiperInstance = null;
             }
             if (thumbsSwiperInstance) {
                 thumbsSwiperInstance.destroy(true, true);
                 thumbsSwiperInstance = null;
             }

             const thumbSlides = container.querySelectorAll('.swiper-thumbs .swiper-slide');
             const thumbCount = thumbSlides.length;

             const thumbsWrapper = container.querySelector(".swiper-thumbs .swiper-wrapper");
             const thumbsPrev = container.querySelector(".thumbs-prev");
             const thumbsNext = container.querySelector(".thumbs-next");

             // Base config
             const thumbsConfig = {
                 spaceBetween: 10,
                 navigation: {
                     nextEl: thumbsNext,
                     prevEl: thumbsPrev,
                 },
                 breakpoints: {
                     320: { slidesPerView: 2 },
                     380: { slidesPerView: 2.6 },
                     450: { slidesPerView: 3 },
                     640: { slidesPerView: 4 },
                     768: { slidesPerView: 5 },
                     1024: { slidesPerView: 5 }
                 }
             };

             if (thumbCount <= 5) {
                 thumbsConfig.slidesPerView = thumbCount;
                 thumbsConfig.loop = false;

                 // Add helper class and hide arrows
                 thumbsWrapper?.classList.add("few-thumbs");
                 if (thumbsPrev) thumbsPrev.style.display = "none";
                 if (thumbsNext) thumbsNext.style.display = "none";
             } else {
                 thumbsConfig.slidesPerView = "auto";
             }

             // Initialize thumbnail swiper
             thumbsSwiperInstance = new Swiper(container.querySelector(".swiper-thumbs"), thumbsConfig);

             // Initialize main swiper
             mainSwiperInstance = new Swiper(container.querySelector(".swiper-main"), {
                 spaceBetween: 10,
                 pagination: {
                     el: container.querySelector(".swiper-pagination"),
                     clickable: true,
                     dynamicBullets: true,
                     dynamicMainBullets: 4,
                 },
                 thumbs: {
                     swiper: thumbsSwiperInstance,
                 },
                 on: {
                     slideChange: function () {
                         dotNetRef?.invokeMethodAsync("UpdateActiveIndex", this.realIndex);
                     }
                 }
             });

             // Thumbnail click = main slider move
             thumbsSwiperInstance.on("click", function (swiper) {
                 if (typeof swiper.clickedIndex !== "undefined") {
                     mainSwiperInstance.slideTo(swiper.clickedIndex);
                 }
             });

             // Manual next/prev using thumbs buttons
             thumbsNext?.addEventListener("click", () => {
                 const nextIndex = Math.min(mainSwiperInstance.realIndex + 1, mainSwiperInstance.slides.length - 1);
                 mainSwiperInstance.slideTo(nextIndex);
             });

             thumbsPrev?.addEventListener("click", () => {
                 const prevIndex = Math.max(mainSwiperInstance.realIndex - 1, 0);
                 mainSwiperInstance.slideTo(prevIndex);
             });

             // Allow external control
             window.goToSlide = (index) => {
                 if (mainSwiperInstance && typeof index === "number") {
                     mainSwiperInstance.slideTo(index);
                 }
             };
         };

         let mainImagePreviewSwiperInstance;
         let thumbsImagePreviewSwiperInstance;

         window.initializeSwiperImagePreview = (dotNetRef, uniqueId) => {
             const container = document.getElementById(uniqueId);
             if (!container) {
                 console.warn("Swiper Image Preview container not found:", uniqueId);
                 return;
             }

             if (mainImagePreviewSwiperInstance) mainImagePreviewSwiperInstance.destroy(true, true);
             if (thumbsImagePreviewSwiperInstance) thumbsImagePreviewSwiperInstance.destroy(true, true);

             const thumbSlides = container.querySelectorAll('.swiper-thumbs-preview .swiper-slide');
             const thumbCount = thumbSlides.length;

             const thumbsConfig = {
                 spaceBetween: 10,
                 navigation: {
                     nextEl: container.querySelector(".thumbs-next-preview"),
                     prevEl: container.querySelector(".thumbs-prev-preview"),
                 },
                 breakpoints: {
                     380: { slidesPerView: 2.6 },
                     450: { slidesPerView: 3 },
                     640: { slidesPerView: 4 },
                     768: { slidesPerView: 5 },
                     1024: { slidesPerView: 5 },
                 }
             };

             if (thumbCount <= 5) {
                 thumbsConfig.slidesPerView = thumbCount;
                 thumbsConfig.loop = false;

                 container.querySelector(".swiper-thumbs-preview .swiper-wrapper")?.classList.add("few-thumbs");

                 container.querySelector(".thumbs-prev-preview").style.display = "none";
                 container.querySelector(".thumbs-next-preview").style.display = "none";
             } else {
                 thumbsConfig.slidesPerView = "auto";
             }

             // Initialize thumbnail swiper
             thumbsImagePreviewSwiperInstance = new Swiper(container.querySelector(".swiper-thumbs-preview"), thumbsConfig);

             // Initialize main swiper
             mainImagePreviewSwiperInstance = new Swiper(container.querySelector(".swiper-main-preview"), {
                 spaceBetween: 10,
                 pagination: {
                     el: container.querySelector(".swiper-pagination-preview"),
                     clickable: true,
                     dynamicBullets: true,
                     dynamicMainBullets: 4,
                 },
                 thumbs: {
                     swiper: thumbsImagePreviewSwiperInstance,
                 },
                 on: {
                     slideChange: function () {
                         dotNetRef?.invokeMethodAsync("UpdateActiveIndex", this.realIndex);
                     }
                 }
             });

             thumbsImagePreviewSwiperInstance.on('click', function (swiper) {
                 if (typeof swiper.clickedIndex !== 'undefined') {
                     mainImagePreviewSwiperInstance.slideTo(swiper.clickedIndex);
                 }
             });

             container.querySelector(".thumbs-next-preview")?.addEventListener("click", () => {
                 const currentIndex = mainImagePreviewSwiperInstance.realIndex;
                 const nextIndex = Math.min(currentIndex + 1, mainImagePreviewSwiperInstance.slides.length - 1);
                 mainImagePreviewSwiperInstance.slideTo(nextIndex);
             });

             container.querySelector(".thumbs-prev-preview")?.addEventListener("click", () => {
                 const currentIndex = mainImagePreviewSwiperInstance.realIndex;
                 const prevIndex = Math.max(currentIndex - 1, 0);
                 mainImagePreviewSwiperInstance.slideTo(prevIndex);
             });

             // External control
             window.goToImagePreviewSlide = (index) => {
                 if (mainImagePreviewSwiperInstance && typeof index === 'number') {
                     mainImagePreviewSwiperInstance.slideTo(index);
                 }
             };
         };