// Inline PayPal Integration for Checkout
(function() {
    'use strict';

    let paypalButtonRendered = false;
    let currentOrderId = null;

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', initializeCheckout);

    function initializeCheckout() {
        console.log('?? PayPal Integration: Initializing...');
        
        const form = document.getElementById('checkoutForm');
        const placeOrderBtn = document.querySelector('.btn-place-order');
        const paymentMethodRadios = document.querySelectorAll('input[name="paymentMethod"]');

        if (!form) {
            console.error('? Checkout form not found!');
            return;
        }

        console.log('? Checkout form found');
        console.log('? Place order button found:', !!placeOrderBtn);
        console.log('? Payment method radios found:', paymentMethodRadios.length);

        // Handle payment method change
        paymentMethodRadios.forEach(radio => {
            radio.addEventListener('change', () => handlePaymentMethodChange(radio.value, placeOrderBtn));
        });

        // Form validation for non-PayPal methods
        form.addEventListener('submit', (e) => validateFormSubmission(e));

        // Initialize with selected method
        const selected = document.querySelector('input[name="paymentMethod"]:checked');
        if (selected) {
            console.log('? Pre-selected payment method:', selected.value);
            handlePaymentMethodChange(selected.value, placeOrderBtn);
        } else {
            console.log('?? No payment method pre-selected');
        }
    }

    function handlePaymentMethodChange(method, placeOrderBtn) {
        console.log('?? Payment method changed to:', method);
        
        let paypalContainer = document.getElementById('paypal-button-container');

        if (method === 'PayPal') {
            console.log('?? PayPal selected, setting up button...');
            
            // Hide regular button
            placeOrderBtn.style.display = 'none';

            // Create PayPal container if needed
            if (!paypalContainer) {
                console.log('?? Creating PayPal container...');
                paypalContainer = createPayPalContainer(placeOrderBtn);
            }
            paypalContainer.style.display = 'block';

            // Render PayPal buttons once
            if (!paypalButtonRendered) {
                console.log('?? Rendering PayPal buttons...');
                renderPayPalButtons(paypalContainer);
                paypalButtonRendered = true;
            } else {
                console.log('? PayPal button already rendered');
            }
        } else {
            console.log('?? Other payment method selected:', method);
            // Show regular button, hide PayPal
            placeOrderBtn.style.display = 'flex';
            if (paypalContainer) {
                paypalContainer.style.display = 'none';
            }
        }
    }

    function createPayPalContainer(placeOrderBtn) {
        const container = document.createElement('div');
        container.id = 'paypal-button-container';
        container.style.minHeight = '150px';
        container.style.marginBottom = 'var(--spacing-md)';
        placeOrderBtn.parentNode.insertBefore(container, placeOrderBtn);
        console.log('? PayPal container created');
        return container;
    }

    // ? NEW: Helper function to validate checkout before PayPal
    function validateCheckoutBeforePayPal() {
        const shippingAddress = document.querySelector('input[name="shippingAddress"]:checked');
        
        if (!shippingAddress) {
            // Scroll to address section for better UX
            const addressSection = document.querySelector('.shipping-addresses') || 
                                   document.querySelector('[data-section="shipping"]') ||
                                   document.querySelector('.address-section') ||
                                   document.querySelector('.address-list');
            if (addressSection) {
                addressSection.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            
            // Highlight the address cards with animation
            const addressCards = document.querySelectorAll('.address-card, .shipping-address-item, .address-option');
            addressCards.forEach(card => {
                // Add highlight class for CSS animation
                card.classList.add('highlight-required');
                
                // Remove class after animation completes
                setTimeout(() => {
                    card.classList.remove('highlight-required');
                }, 1500);
            });
            
            return { valid: false, error: 'Vui lòng ch?n ??a ch? giao hàng tr??c khi thanh toán' };
        }
        
        return { valid: true, addressId: shippingAddress.value };
    }

    function renderPayPalButtons(container) {
        console.log('?? Starting PayPal button render...');
        
        // Check if PayPal SDK is loaded
        if (typeof paypal === 'undefined') {
            console.error('? PayPal SDK not loaded!');
            window.showToast?.('PayPal SDK không ???c t?i. Vui lòng t?i l?i trang.', 'error');
            return;
        }

        console.log('? PayPal SDK loaded successfully');

        paypal.Buttons({
            style: {
                layout: 'vertical',
                color: 'gold',
                shape: 'rect',
                label: 'paypal',
                height: 50
            },

            // ? FIX: onClick handler to validate BEFORE createOrder is called
            onClick: function(data, actions) {
                console.log('??? PayPal button clicked, validating...');
                
                const validation = validateCheckoutBeforePayPal();
                
                if (!validation.valid) {
                    console.warn('?? Validation failed:', validation.error);
                    window.showToast?.(validation.error, 'warning');
                    
                    // Return reject to prevent createOrder from being called
                    return actions.reject();
                }
                
                console.log('? Pre-click validation passed');
                return actions.resolve();
            },

            createOrder: async function() {
                console.log('?? PayPal createOrder called');
                
                // Reset currentOrderId at start of new order creation
                currentOrderId = null;
                
                // Double-check validation (onClick should have caught this, but just in case)
                const validation = validateCheckoutBeforePayPal();
                if (!validation.valid) {
                    console.error('? Validation failed in createOrder');
                    window.showToast?.(validation.error, 'error');
                    throw new Error(validation.error);
                }

                console.log('? Shipping address selected:', validation.addressId);

                try {
                    // Step 1: Create order in backend
                    console.log('?? Step 1: Creating backend order...');
                    const form = document.getElementById('checkoutForm');
                    const formData = new FormData(form);
                    
                    const orderResponse = await fetch('/Checkout/CreateOrderAjax', {
                        method: 'POST',
                        body: formData
                    });

                    console.log('?? Order response status:', orderResponse.status);

                    if (!orderResponse.ok) {
                        const errorText = await orderResponse.text();
                        console.error('? Order creation HTTP error:', orderResponse.status, errorText);
                        window.showToast?.('L?i k?t n?i server. Vui lòng th? l?i.', 'error');
                        throw new Error('Failed to create order: HTTP ' + orderResponse.status);
                    }

                    const orderResult = await orderResponse.json();
                    console.log('?? Order result:', orderResult);

                    if (!orderResult.success) {
                        console.error('? Order creation failed:', orderResult.error);
                        window.showToast?.(orderResult.error || 'Không th? t?o ??n hàng. Vui lòng th? l?i.', 'error');
                        throw new Error(orderResult.error || 'Order creation failed');
                    }

                    // Validate orderId is a valid number
                    const orderId = parseInt(orderResult.orderId, 10);
                    if (!orderId || orderId <= 0 || isNaN(orderId)) {
                        console.error('? Invalid orderId from server:', orderResult);
                        window.showToast?.('L?i: Server tr? v? mã ??n hàng không h?p l?', 'error');
                        throw new Error('Server returned invalid order ID: ' + orderResult.orderId);
                    }

                    currentOrderId = orderId;
                    console.log('? Order ID stored:', currentOrderId);

                    // Step 2: Create PayPal order
                    console.log('?? Step 2: Creating PayPal order...');
                    
                    const paypalResponse = await fetch('/payment/create-paypal-order', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            orderId: currentOrderId,
                            subtotal: orderResult.subtotal,
                            discountAmount: orderResult.discountAmount,
                            totalAmount: orderResult.totalAmount
                        })
                    });

                    console.log('?? PayPal response status:', paypalResponse.status);

                    const paypalOrder = await paypalResponse.json();
                    console.log('?? PayPal order response:', paypalOrder);

                    if (!paypalResponse.ok) {
                        console.error('? PayPal order creation failed:', paypalOrder.error);
                        window.showToast?.(paypalOrder.error || 'L?i t?o thanh toán PayPal', 'error');
                        throw new Error(paypalOrder.error || 'Failed to create PayPal order');
                    }

                    console.log('? PayPal order created successfully:', paypalOrder.id);
                    
                    return paypalOrder.id;
                    
                } catch (error) {
                    console.error('? Error in createOrder:', error);
                    // Don't show duplicate toast if already shown
                    throw error;
                }
            },

            onApprove: async function(data) {
                console.log('? PayPal payment approved:', data);
                console.log('?? Current Order ID:', currentOrderId);
                
                // Validate currentOrderId before proceeding
                if (!currentOrderId || currentOrderId <= 0 || isNaN(currentOrderId)) {
                    console.error('? CRITICAL: Invalid currentOrderId:', currentOrderId);
                    window.showToast?.('L?i nghiêm tr?ng: Không tìm th?y mã ??n hàng. Vui lòng ki?m tra danh sách ??n hàng.', 'error');
                    setTimeout(() => {
                        window.location.href = '/Order';
                    }, 3000);
                    return;
                }
                
                window.showToast?.('?ang x? lý thanh toán...', 'info');

                try {
                    const captureUrl = `/payment/capture-paypal-order?orderId=${data.orderID}&orderIdLocal=${currentOrderId}`;
                    console.log('?? Capturing payment:', captureUrl);
                    
                    const response = await fetch(captureUrl, { method: 'POST' });

                    console.log('?? Capture response status:', response.status);

                    if (!response.ok) {
                        const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
                        console.error('? Capture failed:', errorData);
                        throw new Error(errorData.error || 'Payment capture failed');
                    }

                    const captureData = await response.json();
                    console.log('? Payment captured successfully:', captureData);
                    
                    // Redirect to success page
                    if (currentOrderId && currentOrderId > 0) {
                        const successUrl = `/Payment/PayPalSuccess?orderId=${currentOrderId}&token=${data.orderID}`;
                        console.log('?? Redirecting to success page:', successUrl);
                        window.location.href = successUrl;
                    } else {
                        window.showToast?.('Thanh toán thành công! ?ang chuy?n ??n trang ??n hàng...', 'success');
                        setTimeout(() => {
                            window.location.href = '/Order';
                        }, 2000);
                    }
                    
                } catch (error) {
                    console.error('? Error capturing payment:', error);
                    window.showToast?.('L?i x? lý thanh toán: ' + error.message, 'error');
                }
            },

            onCancel: function(data) {
                console.log('?? PayPal payment cancelled by user:', data);
                window.showToast?.('B?n ?ã h?y thanh toán PayPal', 'warning');
            },

            onError: function(err) {
                // ? FIX: Better error handling - don't log validation errors as critical
                const errorMessage = err?.message || String(err);
                
                // Check if this is a validation error (user didn't select address)
                if (errorMessage.includes('address') || errorMessage.includes('??a ch?')) {
                    console.warn('?? PayPal validation error:', errorMessage);
                    // Toast already shown by onClick or createOrder
                    return;
                }
                
                console.error('? PayPal SDK error:', err);
                window.showToast?.('Có l?i x?y ra v?i PayPal. Vui lòng th? l?i.', 'error');
            }
        }).render(container).then(() => {
            console.log('? PayPal button rendered successfully');
        }).catch((err) => {
            console.error('? Failed to render PayPal button:', err);
            window.showToast?.('Không th? t?i nút PayPal. Vui lòng t?i l?i trang.', 'error');
        });
    }

    function validateFormSubmission(e) {
        const shippingAddress = document.querySelector('input[name="shippingAddress"]:checked');
        const paymentMethod = document.querySelector('input[name="paymentMethod"]:checked');

        if (!shippingAddress) {
            e.preventDefault();
            window.showToast?.('Vui lòng ch?n ??a ch? giao hàng', 'error');
            return false;
        }

        if (!paymentMethod) {
            e.preventDefault();
            window.showToast?.('Vui lòng ch?n ph??ng th?c thanh toán', 'error');
            return false;
        }

        // Prevent form submission for PayPal (handled by button)
        if (paymentMethod.value === 'PayPal') {
            e.preventDefault();
            window.showToast?.('Vui lòng s? d?ng nút PayPal bên d??i', 'error');
            return false;
        }
    }

})();
