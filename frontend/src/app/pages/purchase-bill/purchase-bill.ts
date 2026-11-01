import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

interface Location {
  locationCode: string;
  locationName: string;
}

interface BillItem {
  id: number;
  item: string;
  batch: string;
  locationCode: string;
  standardCost: number;
  standardPrice: number;
  quantity: number;
  discount: number;
  totalCost: number;
  totalSelling: number;
}

@Component({
  selector: 'app-purchase-bill',
  imports: [ReactiveFormsModule],
  templateUrl: './purchase-bill.html',
  styleUrl: './purchase-bill.css'
})
export class PurchaseBill implements OnInit {

  private http = inject(HttpClient);
  private nextId = 1;
  private router = inject(Router);

  loggingOut = signal(false);
  logoutError = signal('');

  // Available items
  fruits = [
    'Mango', 'Apple', 'Banana', 'Orange',
    'Grapes', 'Kiwi', 'Strawberry'
  ];

  // Data from SQL Server
  locations = signal<Location[]>([]);
  loading = signal(true);
  error = signal('');
  formError = signal('');

  // Items added to the bill
  items = signal<BillItem[]>([]);

  saving = signal(false);
  saveMessage = signal('');
  saveError = signal('');

  totalItems = computed(() => this.items().length);

  totalQuantity = computed(() =>
    this.items().reduce((sum, item) => sum + item.quantity, 0)
  );

  calculated = signal({
    totalCost: 0,
    totalSelling: 0
  });

  billForm = new FormGroup({
    item: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    batch: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    standardCost: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)]
    }),
    standardPrice: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)]
    }),
    quantity: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    }),
    discount: new FormControl(0, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(0),
        Validators.max(100)
      ]
    })
  });

  constructor() {
    // Recalculate when the form values change
    this.billForm.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.calculate());
  }

  ngOnInit(): void {
    // Get saved locations from our backend
    this.http.get<Location[]>('/api/locations').subscribe({
      next: (data) => {
        this.locations.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load locations.');
        this.loading.set(false);
      }
    });
  }

  calculate(): void {
    const values = this.billForm.getRawValue();

    const totalCost =
      values.standardCost *
      values.quantity *
      (1 - values.discount / 100);

    const totalSelling =
      values.standardPrice * values.quantity;

    this.calculated.set({
      totalCost: Math.round(totalCost * 100) / 100,
      totalSelling: Math.round(totalSelling * 100) / 100
    });
  }

  addItem(): void {
    this.formError.set('');
    this.saveMessage.set('');

    if (this.saving()) {
      return;
    }

    if (this.billForm.invalid) {
      this.billForm.markAllAsTouched();
      this.formError.set('Please enter valid values in all fields.');
      return;
    }

    const values = this.billForm.getRawValue();

    const selectedFruit = this.fruits.find(
      fruit => fruit.toLowerCase() === values.item.trim().toLowerCase()
    );

    if (!selectedFruit) {
      this.formError.set('Please select an item from the suggestions.');
      return;
    }

    const location = this.locations().find(
      loc => loc.locationCode === values.batch
    );

    if (!location) {
      this.formError.set('Please select a valid batch.');
      return;
    }

    const newItem: BillItem = {
      id: this.nextId++,
      item: selectedFruit,
      batch: location.locationName,
      locationCode: location.locationCode,
      standardCost: values.standardCost,
      standardPrice: values.standardPrice,
      quantity: values.quantity,
      discount: values.discount,
      totalCost: this.calculated().totalCost,
      totalSelling: this.calculated().totalSelling
    };

    this.items.update(current => [...current, newItem]);

    // Clear form after adding an item
    this.billForm.reset({
      item: '',
      batch: '',
      standardCost: 0,
      standardPrice: 0,
      quantity: 1,
      discount: 0
    });
  }


  saveBill(): void {

    if (this.items().length === 0 || this.saving()) {
      return;
    }

    this.saving.set(true);
    this.saveMessage.set('');
    this.saveError.set('');

    // Prepare the items for our C# API
    const billData = {
      items: this.items().map(item => ({
        item: item.item,
        locationCode: item.locationCode,
        standardCost: item.standardCost,
        standardPrice: item.standardPrice,
        quantity: item.quantity,
        discount: item.discount
      }))
    };

    // Send Purchase Bill to ASP.NET Core
    this.http.post<{
      message: string;
      billId: string;
      totalItems: number;
      totalQuantity: number;
    }>('/api/purchasebills', billData)
      .subscribe({
        next: (response) => {

          this.saveMessage.set(response.message);

          console.log('Saved Bill ID:', response.billId);

          // Clear the table after successful saving
          this.items.set([]);

          this.saving.set(false);
        },

        error: (error) => {

          this.saveError.set(
            error.error?.message ||
            'Could not save the Purchase Bill.'
          );

          this.saving.set(false);
        }
      });
  }

  logout(): void {

    if (this.loggingOut()) {
      return;
    }

    this.loggingOut.set(true);
    this.logoutError.set('');

    this.http.post('/api/auth/logout', {}).subscribe({

      next: () => {
        // Clear current items from memory
        this.items.set([]);

        // Redirect to login
        this.router.navigate(['/login'], {
          replaceUrl: true
        });
      },

      error: () => {
        this.logoutError.set('Logout failed. Please try again.');
        this.loggingOut.set(false);
      }

    });
  }
}
