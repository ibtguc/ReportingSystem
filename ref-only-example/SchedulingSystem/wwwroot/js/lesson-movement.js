/**
 * Lesson Movement Module
 * Provides drag-and-drop, available slots finder, and swap chain suggestion features
 */

class LessonMovementManager {
    constructor(timetableId) {
        this.timetableId = timetableId;
        this.draggedLesson = null;
        this.excludedSlots = new Set();
        this.init();
    }

    init() {
        this.initDragAndDrop();
        this.initContextMenu();
        this.initModals();
    }

    // ==================== DRAG AND DROP ====================

    initDragAndDrop() {
        const lessonCards = document.querySelectorAll('.lesson-card');
        const timetableCells = document.querySelectorAll('.table td');

        lessonCards.forEach(card => {
            card.setAttribute('draggable', 'true');
            card.style.cursor = 'grab';

            card.addEventListener('dragstart', (e) => this.handleDragStart(e));
            card.addEventListener('dragend', (e) => this.handleDragEnd(e));
        });

        timetableCells.forEach(cell => {
            cell.addEventListener('dragover', (e) => this.handleDragOver(e));
            cell.addEventListener('drop', (e) => this.handleDrop(e));
            cell.addEventListener('dragleave', (e) => this.handleDragLeave(e));
        });
    }

    handleDragStart(e) {
        const card = e.target.closest('.lesson-card');
        this.draggedLesson = {
            scheduledId: parseInt(card.querySelector('.edit-lesson-btn')?.dataset.scheduledId),
            element: card
        };

        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/html', card.innerHTML);

        card.style.opacity = '0.4';
        card.style.cursor = 'grabbing';
    }

    handleDragEnd(e) {
        e.target.style.opacity = '1';
        e.target.style.cursor = 'grab';

        // Remove all drop zone highlights
        document.querySelectorAll('.drop-zone-valid, .drop-zone-invalid, .drop-zone-hover')
            .forEach(el => {
                el.classList.remove('drop-zone-valid', 'drop-zone-invalid', 'drop-zone-hover');
            });
    }

    handleDragOver(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';

        const cell = e.currentTarget;
        cell.classList.add('drop-zone-hover');

        // TODO: Add real-time validation feedback
        // For now, just allow drop
        return false;
    }

    handleDragLeave(e) {
        const cell = e.currentTarget;
        cell.classList.remove('drop-zone-hover');
    }

    async handleDrop(e) {
        e.stopPropagation();
        e.preventDefault();

        const cell = e.currentTarget;
        cell.classList.remove('drop-zone-hover');

        if (!this.draggedLesson) return;

        // Get target slot info from table structure
        const row = cell.parentElement;
        const cellIndex = Array.from(row.children).indexOf(cell);

        if (cellIndex === 0) return; // First column is time label

        const dayOfWeek = this.getDayOfWeekFromIndex(cellIndex - 1);
        const periodId = this.getPeriodIdFromRow(row);

        // Get room ID from first lesson in cell if any, or null
        const firstLessonInCell = cell.querySelector('.lesson-card');
        const roomId = firstLessonInCell?.querySelector('.edit-lesson-btn')?.dataset.roomId || null;

        // Validate and move
        await this.validateAndMove(
            this.draggedLesson.scheduledId,
            dayOfWeek,
            periodId,
            roomId
        );

        this.draggedLesson = null;
    }

    getDayOfWeekFromIndex(index) {
        const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday'];
        return days[index];
    }

    getPeriodIdFromRow(row) {
        // Period ID is stored in the first lesson card's data attribute in this row
        const firstCard = row.querySelector('.lesson-card');
        if (firstCard) {
            const periodId = firstCard.querySelector('.edit-lesson-btn')?.dataset.periodId;
            if (periodId) return parseInt(periodId);
        }

        // Fallback: get from row index
        const tbody = row.parentElement;
        const rowIndex = Array.from(tbody.children).indexOf(row);
        return rowIndex + 1; // Assuming period IDs start from 1
    }

    async validateAndMove(scheduledLessonId, targetDay, targetPeriodId, targetRoomId) {
        try {
            // Show loading indicator
            this.showLoadingOverlay('Validating move...');

            // First, determine strategy
            const strategy = await this.getMovementStrategy(
                scheduledLessonId,
                targetDay,
                targetPeriodId,
                targetRoomId
            );

            this.hideLoadingOverlay();

            if (strategy.canMoveDirectly) {
                // Direct move possible
                if (strategy.validation && strategy.validation.qualityScore >= 70) {
                    // Good move, execute directly
                    await this.executeDirectMove(scheduledLessonId, targetDay, targetPeriodId, targetRoomId);
                } else {
                    // Show warnings and ask for confirmation
                    const confirmed = await this.showMoveConfirmation(strategy.validation);
                    if (confirmed) {
                        await this.executeDirectMove(scheduledLessonId, targetDay, targetPeriodId, targetRoomId);
                    }
                }
            } else if (strategy.requiresSwaps) {
                // Show swap chain suggestion dialog
                await this.showSwapChainDialog(scheduledLessonId, targetDay, targetPeriodId, targetRoomId);
            } else {
                // Move not possible
                this.showError(strategy.errorMessage || 'Cannot move lesson to this slot');
            }
        } catch (error) {
            this.hideLoadingOverlay();
            this.showError(`Error: ${error.message}`);
        }
    }

    async executeDirectMove(scheduledLessonId, targetDay, targetPeriodId, targetRoomId) {
        // Use existing OnPostEditLessonAsync endpoint
        const formData = new FormData();
        formData.append('timetableId', this.timetableId);
        formData.append('scheduledLessonId', scheduledLessonId);
        formData.append('dayOfWeek', targetDay);
        formData.append('periodId', targetPeriodId);
        if (targetRoomId) {
            formData.append('roomId', targetRoomId);
        }

        const response = await fetch('?handler=EditLesson', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });

        if (response.ok) {
            // Reload page to show updated timetable
            window.location.reload();
        } else {
            throw new Error('Failed to move lesson');
        }
    }

    // ==================== AVAILABLE SLOTS DIALOG ====================

    async showAvailableSlotsDialog(scheduledLessonId) {
        try {
            this.showLoadingOverlay('Finding available slots...');

            const excludeSlotsParam = this.getExcludeSlotsParam();
            const response = await fetch(
                `?handler=AvailableSlotsGrouped&scheduledLessonId=${scheduledLessonId}&excludeSlots=${encodeURIComponent(excludeSlotsParam)}`
            );

            if (!response.ok) throw new Error('Failed to load available slots');

            const data = await response.json();
            this.hideLoadingOverlay();

            if (!data.success) {
                this.showError(data.error);
                return;
            }

            this.populateAvailableSlotsModal(scheduledLessonId, data);
            const modal = new bootstrap.Modal(document.getElementById('availableSlotsModal'));
            modal.show();
        } catch (error) {
            this.hideLoadingOverlay();
            this.showError(`Error: ${error.message}`);
        }
    }

    populateAvailableSlotsModal(scheduledLessonId, data) {
        const modalBody = document.getElementById('availableSlotsContent');

        let html = `<p class="text-muted">Found ${data.totalAvailable} available slot(s)</p>`;

        // Perfect slots
        if (data.perfect && data.perfect.length > 0) {
            html += this.renderSlotGroup('Perfect Slots', data.perfect, 'success', scheduledLessonId);
        }

        // Good slots
        if (data.good && data.good.length > 0) {
            html += this.renderSlotGroup('Good Slots', data.good, 'info', scheduledLessonId);
        }

        // Acceptable slots
        if (data.acceptable && data.acceptable.length > 0) {
            html += this.renderSlotGroup('Acceptable Slots', data.acceptable, 'warning', scheduledLessonId);
        }

        // Poor slots
        if (data.poor && data.poor.length > 0) {
            html += this.renderSlotGroup('Poor Slots (has soft violations)', data.poor, 'danger', scheduledLessonId);
        }

        if (data.totalAvailable === 0) {
            html += '<div class="alert alert-warning">No available slots found. Try removing some excluded slots.</div>';
        }

        modalBody.innerHTML = html;

        // Add click handlers for slot buttons
        modalBody.querySelectorAll('.move-to-slot-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const button = e.currentTarget;
                const day = button.dataset.day;
                const periodId = parseInt(button.dataset.periodId);
                const roomId = button.dataset.roomId ? parseInt(button.dataset.roomId) : null;

                // Close modal
                bootstrap.Modal.getInstance(document.getElementById('availableSlotsModal')).hide();

                // Execute move
                await this.executeDirectMove(scheduledLessonId, day, periodId, roomId);
            });
        });
    }

    renderSlotGroup(title, slots, badgeClass, scheduledLessonId) {
        let html = `
            <div class="mb-3">
                <h6><span class="badge bg-${badgeClass}">${slots.length}</span> ${title}</h6>
                <div class="list-group">
        `;

        slots.forEach(slot => {
            const violations = slot.softViolations && slot.softViolations.length > 0
                ? `<small class="text-warning"><br>⚠️ ${slot.softViolations.map(v => v.message).join(', ')}</small>`
                : '';

            html += `
                <div class="list-group-item d-flex justify-content-between align-items-center">
                    <div>
                        <strong>${slot.dayOfWeek}</strong> - ${slot.periodName}
                        ${slot.roomName ? `<span class="badge bg-secondary ms-2">${slot.roomName}</span>` : ''}
                        <small class="text-muted ms-2">(Score: ${slot.qualityScore.toFixed(0)})</small>
                        ${violations}
                    </div>
                    <button class="btn btn-sm btn-primary move-to-slot-btn"
                            data-scheduled-id="${scheduledLessonId}"
                            data-day="${slot.dayOfWeek}"
                            data-period-id="${slot.periodId}"
                            data-room-id="${slot.roomId || ''}">
                        Move Here
                    </button>
                </div>
            `;
        });

        html += `
                </div>
            </div>
        `;

        return html;
    }

    // ==================== SWAP CHAIN DIALOG ====================

    async showSwapChainDialog(scheduledLessonId, targetDay, targetPeriodId, targetRoomId) {
        try {
            this.showLoadingOverlay('Finding swap sequences... (this may take up to 30 seconds)');

            const excludeSlotsParam = this.getExcludeSlotsParam();
            const response = await fetch(
                `?handler=FindSwapChains&scheduledLessonId=${scheduledLessonId}` +
                `&targetDay=${targetDay}&targetPeriodId=${targetPeriodId}` +
                `&targetRoomId=${targetRoomId || ''}` +
                `&maxDepth=3&timeoutSeconds=30&excludeSlots=${encodeURIComponent(excludeSlotsParam)}`
            );

            if (!response.ok) throw new Error('Failed to find swap chains');

            const data = await response.json();
            this.hideLoadingOverlay();

            if (!data.success) {
                this.showError(data.error);
                return;
            }

            if (!data.swapChains || data.swapChains.length === 0) {
                this.showError('No valid swap sequences found. The move may not be possible.');
                return;
            }

            this.populateSwapChainModal(data.swapChains);
            const modal = new bootstrap.Modal(document.getElementById('swapChainModal'));
            modal.show();
        } catch (error) {
            this.hideLoadingOverlay();
            this.showError(`Error: ${error.message}`);
        }
    }

    populateSwapChainModal(swapChains) {
        const modalBody = document.getElementById('swapChainContent');

        let html = `<p class="text-muted">Found ${swapChains.length} possible sequence(s)</p>`;

        swapChains.forEach((chain, index) => {
            if (!chain.isValid) {
                html += `
                    <div class="alert alert-danger">
                        <strong>Sequence ${index + 1}:</strong> ${chain.errorMessage}
                    </div>
                `;
                return;
            }

            const badgeClass = chain.totalMoves === 1 ? 'success' : chain.totalMoves === 2 ? 'info' : 'warning';

            html += `
                <div class="card mb-3">
                    <div class="card-header bg-light">
                        <strong>Sequence ${index + 1}</strong>
                        <span class="badge bg-${badgeClass} ms-2">${chain.totalMoves} move(s)</span>
                        <span class="badge bg-secondary ms-1">Score: ${chain.qualityScore.toFixed(0)}</span>
                    </div>
                    <div class="card-body">
                        <ol class="mb-3">
            `;

            chain.steps.forEach(step => {
                html += `
                    <li class="mb-2">
                        <strong>${step.lessonDescription}</strong><br>
                        <small class="text-muted">
                            From: ${step.from.day} ${step.from.periodName} ${step.from.roomName ? `(${step.from.roomName})` : ''}
                            →
                            To: ${step.to.day} ${step.to.periodName} ${step.to.roomName ? `(${step.to.roomName})` : ''}
                        </small>
                    </li>
                `;
            });

            html += `
                        </ol>
                        <button class="btn btn-primary execute-swap-btn" data-chain-index="${index}">
                            <i class="bi bi-check-circle"></i> Execute This Sequence
                        </button>
                    </div>
                </div>
            `;
        });

        modalBody.innerHTML = html;

        // Add click handlers for execute buttons
        modalBody.querySelectorAll('.execute-swap-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const chainIndex = parseInt(e.currentTarget.dataset.chainIndex);
                const chain = swapChains[chainIndex];

                // Close modal
                bootstrap.Modal.getInstance(document.getElementById('swapChainModal')).hide();

                // Execute swap chain
                await this.executeSwapChain(chain);
            });
        });
    }

    async executeSwapChain(swapChain) {
        try {
            this.showLoadingOverlay('Executing swap sequence...');

            const response = await fetch('?handler=ExecuteSwapChain', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    steps: swapChain.steps,
                    force: false
                })
            });

            if (!response.ok) throw new Error('Failed to execute swap chain');

            const data = await response.json();
            this.hideLoadingOverlay();

            if (data.success) {
                this.showSuccess(`Successfully executed ${data.totalMoves} move(s)!`);
                setTimeout(() => window.location.reload(), 1500);
            } else {
                this.showError(data.error);
            }
        } catch (error) {
            this.hideLoadingOverlay();
            this.showError(`Error: ${error.message}`);
        }
    }

    // ==================== EXCLUDE SLOTS FUNCTIONALITY ====================

    toggleExcludeSlot(day, periodId) {
        const key = `${day},${periodId}`;
        if (this.excludedSlots.has(key)) {
            this.excludedSlots.delete(key);
        } else {
            this.excludedSlots.add(key);
        }
        this.updateExcludedSlotsUI();
    }

    clearExcludedSlots() {
        this.excludedSlots.clear();
        this.updateExcludedSlotsUI();
    }

    getExcludeSlotsParam() {
        return Array.from(this.excludedSlots).join(';');
    }

    updateExcludedSlotsUI() {
        // Update cell backgrounds
        document.querySelectorAll('.table td').forEach(cell => {
            cell.classList.remove('excluded-slot');
        });

        this.excludedSlots.forEach(slotKey => {
            const [day, periodId] = slotKey.split(',');
            const cell = this.findCellByDayAndPeriod(day, parseInt(periodId));
            if (cell) {
                cell.classList.add('excluded-slot');
            }
        });

        // Update counter
        const counter = document.getElementById('excludedSlotsCount');
        if (counter) {
            if (this.excludedSlots.size > 0) {
                counter.textContent = `${this.excludedSlots.size} slot(s) excluded`;
                counter.classList.remove('d-none');
            } else {
                counter.classList.add('d-none');
            }
        }
    }

    findCellByDayAndPeriod(day, periodId) {
        // Implementation depends on table structure
        // This is a simplified version
        return null; // TODO: Implement based on actual table structure
    }

    // ==================== CONTEXT MENU ====================

    initContextMenu() {
        // Add context menu to lesson cards
        document.querySelectorAll('.lesson-card').forEach(card => {
            card.addEventListener('contextmenu', (e) => {
                e.preventDefault();
                this.showLessonContextMenu(e, card);
            });
        });
    }

    showLessonContextMenu(e, card) {
        const scheduledId = parseInt(card.querySelector('.edit-lesson-btn')?.dataset.scheduledId);
        if (!scheduledId) return;

        // Remove any existing context menu
        document.querySelector('.lesson-context-menu')?.remove();

        // Create context menu
        const menu = document.createElement('div');
        menu.className = 'lesson-context-menu';
        menu.style.position = 'fixed';
        menu.style.left = e.pageX + 'px';
        menu.style.top = e.pageY + 'px';
        menu.style.zIndex = '10000';
        menu.style.background = 'white';
        menu.style.border = '1px solid #ccc';
        menu.style.borderRadius = '4px';
        menu.style.boxShadow = '0 2px 10px rgba(0,0,0,0.2)';
        menu.style.padding = '5px 0';

        menu.innerHTML = `
            <div class="context-menu-item" data-action="findSlots" style="padding: 8px 16px; cursor: pointer;">
                <i class="bi bi-search"></i> Find Available Slots
            </div>
            <div class="context-menu-item" data-action="moveHere" style="padding: 8px 16px; cursor: pointer;">
                <i class="bi bi-cursor"></i> Move to Clicked Slot
            </div>
            <hr style="margin: 5px 0;">
            <div class="context-menu-item" data-action="edit" style="padding: 8px 16px; cursor: pointer;">
                <i class="bi bi-pencil"></i> Edit
            </div>
        `;

        // Add hover effect
        menu.querySelectorAll('.context-menu-item').forEach(item => {
            item.addEventListener('mouseenter', () => {
                item.style.backgroundColor = '#f0f0f0';
            });
            item.addEventListener('mouseleave', () => {
                item.style.backgroundColor = 'transparent';
            });
            item.addEventListener('click', async () => {
                const action = item.dataset.action;
                menu.remove();

                if (action === 'findSlots') {
                    await this.showAvailableSlotsDialog(scheduledId);
                } else if (action === 'edit') {
                    // Trigger existing edit button click
                    card.querySelector('.edit-lesson-btn')?.click();
                }
            });
        });

        document.body.appendChild(menu);

        // Remove menu on click outside
        setTimeout(() => {
            document.addEventListener('click', () => menu.remove(), { once: true });
        }, 100);
    }

    // ==================== API CALLS ====================

    async getMovementStrategy(scheduledLessonId, targetDay, targetPeriodId, targetRoomId) {
        const response = await fetch(
            `?handler=MovementStrategy&scheduledLessonId=${scheduledLessonId}` +
            `&targetDay=${targetDay}&targetPeriodId=${targetPeriodId}` +
            `&targetRoomId=${targetRoomId || ''}`
        );

        if (!response.ok) throw new Error('Failed to get movement strategy');

        const data = await response.json();
        if (!data.success) throw new Error(data.error);

        return data;
    }

    // ==================== UI HELPERS ====================

    showLoadingOverlay(message) {
        let overlay = document.getElementById('loadingOverlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'loadingOverlay';
            overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.7);
                display: flex;
                justify-content: center;
                align-items: center;
                z-index: 10000;
            `;
            overlay.innerHTML = `
                <div style="background: white; padding: 30px; border-radius: 10px; text-align: center;">
                    <div class="spinner-border text-primary mb-3" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <div id="loadingMessage">Loading...</div>
                </div>
            `;
            document.body.appendChild(overlay);
        }

        document.getElementById('loadingMessage').textContent = message;
        overlay.style.display = 'flex';
    }

    hideLoadingOverlay() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.style.display = 'none';
        }
    }

    showError(message) {
        // Use Bootstrap toast or alert
        alert('Error: ' + message);
    }

    showSuccess(message) {
        // Use Bootstrap toast or alert
        alert(message);
    }

    async showMoveConfirmation(validation) {
        const warnings = validation.softViolations
            .map(v => `• ${v.message}`)
            .join('\n');

        return confirm(
            `This move has soft constraint violations:\n\n${warnings}\n\nDo you want to proceed?`
        );
    }

    // ==================== MODAL INITIALIZATION ====================

    initModals() {
        // Modals will be initialized when they are shown
    }
}

// Initialize when page loads
document.addEventListener('DOMContentLoaded', function() {
    // Get timetable ID from page
    const timetableIdElement = document.querySelector('[data-timetable-id]');
    if (timetableIdElement) {
        const timetableId = parseInt(timetableIdElement.dataset.timetableId);
        window.lessonMovementManager = new LessonMovementManager(timetableId);
    }
});
