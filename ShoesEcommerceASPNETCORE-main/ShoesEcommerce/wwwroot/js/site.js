// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Site-wide JavaScript for ShoesEcommerce
// Handles: Add to Cart, Login Modal, Cart Count, Toast Notifications

document.addEventListener('DOMContentLoaded', function() {
    // Initialize
    initAddToCartButtons();
    updateCartCount();
});

// ============================================
// ADD TO CART FUNCTIONALITY
// ============================================
function initAddToCartButtons() {
    document.querySelectorAll('.btn-add-cart:not(.disabled)').forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const variantId = this.dataset.variantId;
            const productId = this.dataset.productId;
            
            if (!variantId) {
                // If no variant, redirect to product page to select variant
                if (productId) {
                    window.location.href = `/Product/Details/${productId}`;
                }
                return;
            }
            
            addToCart(variantId, 1, this);
        });
    });
}

async function addToCart(variantId, quantity, buttonEl) {
    // Check if user is authenticated
    const isAuthenticated = await checkAuthentication();
    
    if (!isAuthenticated) {
        showLoginModal('Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng');
        return;
    }
    
    // Disable button and show loading
    if (buttonEl) {
        buttonEl.disabled = true;
        buttonEl.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang thêm...';
    }
    
    try {
        const formData = new FormData();
        formData.append('productVariantId', variantId);
        formData.append('quantity', quantity);
        
        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            formData.append('__RequestVerificationToken', token.value);
        }
        
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            body: formData
        });
        
        if (response.ok) {
            const result = await response.text();
            let data;
            try {
                data = JSON.parse(result);
            } catch {
                data = { success: true };
            }
            
            if (data.success !== false) {
                showToast('Đã thêm sản phẩm vào giỏ hàng!', 'success');
                updateCartCount();
            } else {
                showToast(data.message || 'Có lỗi xảy ra', 'error');
            }
        } else if (response.status === 401) {
            showLoginModal('Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng');
        } else {
            const errorData = await response.json().catch(() => ({}));
            showToast(errorData.message || 'Có lỗi xảy ra khi thêm vào giỏ hàng', 'error');
        }
    } catch (error) {
        console.error('Add to cart error:', error);
        showToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
    } finally {
        // Restore button
        if (buttonEl) {
            buttonEl.disabled = false;
            buttonEl.innerHTML = '<i class="fas fa-shopping-cart"></i> Thêm vào giỏ';
        }
    }
}

// ============================================
// AUTHENTICATION CHECK
// ============================================
async function checkAuthentication() {
    try {
        const response = await fetch('/Account/IsAuthenticated', {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
        });
        
        if (response.ok) {
            const data = await response.json();
            return data.isAuthenticated === true;
        }
        
        // If endpoint doesn't exist, check via cart count as fallback
        return window.currentUserId && window.currentUserId !== 'anonymous';
    } catch {
        return window.currentUserId && window.currentUserId !== 'anonymous';
    }
}

// ============================================
// LOGIN MODAL
// ============================================
function showLoginModal(message) {
    // Remove existing modal if any
    const existingModal = document.getElementById('globalLoginModal');
    if (existingModal) {
        existingModal.remove();
    }
    
    const currentPath = window.location.pathname + window.location.search;
    const encodedReturnUrl = encodeURIComponent(currentPath);
    
    const modalHtml = `
        <div class="login-modal-overlay" id="globalLoginModal">
            <div class="login-modal-box">
                <button type="button" class="login-modal-close" onclick="closeLoginModal()">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                    </svg>
                </button>
                <div class="login-modal-icon">
                    <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                        <circle cx="12" cy="7" r="4"/>
                    </svg>
                </div>
                <h3>Đăng nhập để tiếp tục</h3>
                <p>${message || 'Bạn cần đăng nhập để thực hiện thao tác này'}</p>
                <div class="login-modal-actions">
                    <a href="/Account/Login?returnUrl=${encodedReturnUrl}" class="btn-login-primary">Đăng nhập</a>
                    <a href="/Account/Register" class="btn-register-secondary">Đăng ký tài khoản</a>
                </div>
            </div>
        </div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    document.body.style.overflow = 'hidden';
    
    // Close on overlay click
    document.getElementById('globalLoginModal').addEventListener('click', function(e) {
        if (e.target === this) {
            closeLoginModal();
        }
    });
    
    // Close on Escape key
    document.addEventListener('keydown', handleEscapeKey);
}

function closeLoginModal() {
    const modal = document.getElementById('globalLoginModal');
    if (modal) {
        modal.remove();
        document.body.style.overflow = '';
        document.removeEventListener('keydown', handleEscapeKey);
    }
}

function handleEscapeKey(e) {
    if (e.key === 'Escape') {
        closeLoginModal();
    }
}

// ============================================
// CART COUNT
// ============================================
function updateCartCount() {
    fetch('/Cart/GetCartCount')
        .then(response => response.json())
        .then(data => {
            const badge = document.getElementById('cartCount');
            if (badge && data.count !== undefined) {
                badge.textContent = data.count;
                badge.style.display = data.count > 0 ? 'flex' : 'none';
            }
        })
        .catch(() => {});
}

// ============================================
// TOAST NOTIFICATIONS
// ============================================
function showToast(message, type = 'info') {
    // Remove existing toasts
    document.querySelectorAll('.site-toast').forEach(t => t.remove());
    
    const iconMap = {
        success: '<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><path d="M22 4 12 14.01l-3-3"/>',
        error: '<circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>',
        warning: '<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>',
        info: '<circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/>',
        default: '<circle cx="12" cy="12" r="10"/>'
    };
    
    const toast = document.createElement('div');
    toast.className = `site-toast site-toast-${type}`;
    toast.innerHTML = `
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            ${iconMap[type] || iconMap.default}
        </svg>
        <span>${message}</span>
        <button onclick="this.parentElement.remove()" class="toast-close">&times;</button>
    `;
    
    document.body.appendChild(toast);
    
    // Trigger animation
    requestAnimationFrame(() => {
        toast.classList.add('show');
    });
    
    // Auto remove
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// Make functions globally available
window.addToCart = addToCart;
window.showLoginModal = showLoginModal;
window.closeLoginModal = closeLoginModal;
window.showToast = showToast;
window.updateCartCount = updateCartCount;
