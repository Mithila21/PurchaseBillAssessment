
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

interface PurchaseOrder {
  purchaseOrderId: string;
  netAmount: number;
  numberOfItems: number;
}

interface PurchaseItem {
  purchaseOrderId: string;
  itemName: string;
  quantity: number;
}

interface ItemSummary {
  itemName: string;
  totalQuantity: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {

  private http = inject(HttpClient);

  orders = signal<PurchaseOrder[]>([]);
  oldestItems = signal<PurchaseItem[]>([]);
  summary = signal<ItemSummary[]>([]);

  loading = signal(false);
  error = signal('');

  private colors = [
    '#06b6d4',
    '#2563eb',
    '#8b5cf6',
    '#f97316',
    '#22c55e',
    '#ec4899',
    '#eab308'
  ];

  totalQuantity = computed(() =>
    this.summary().reduce(
      (total, item) => total + item.totalQuantity, 0
    )
  );

  chartItems = computed(() => {
    const total = this.totalQuantity();

    return this.summary().map((item, index) => ({
      ...item,
      color: this.colors[index % this.colors.length],
      percentage: total > 0
        ? (item.totalQuantity / total) * 100
        : 0
    }));
  });

  donutBackground = computed(() => {
    const items = this.chartItems();

    if (!items.length) {
      return '#e5e7eb';
    }

    let position = 0;

    const segments = items.map(item => {
      const start = position;
      position += item.percentage;

      return `${item.color} ${start}% ${position}%`;
    });

    return `conic-gradient(${segments.join(', ')})`;
  });

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      orders: this.http.get<PurchaseOrder[]>(
        '/api/dashboard/latest-orders'
      ),
      items: this.http.get<PurchaseItem[]>(
        '/api/dashboard/oldest-items'
      ),
      summary: this.http.get<ItemSummary[]>(
        '/api/dashboard/item-summary'
      )
    }).subscribe({
      next: (result) => {
        this.orders.set(result.orders);
        this.oldestItems.set(result.items);
        this.summary.set(result.summary);
        this.loading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('Failed to load dashboard data.');
        this.loading.set(false);
      }
    });
  }
}