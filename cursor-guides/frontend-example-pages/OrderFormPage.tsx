import { useState } from "react";
import { useOrder, useCreateOrder, useUpdateOrder } from "@/hooks/useOrders";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Loader2 } from "lucide-react";

const orderFormSchema = z.object({
  tbbn: z.string().min(3, "TBBN is required"),
  customerName: z.string().optional(),
  customerPhone: z.string().optional(),
  addressLine1: z.string().optional(),
  city: z.string().optional(),
  state: z.string().optional(),
  postcode: z.string().optional(),
});

type OrderFormValues = z.infer<typeof orderFormSchema>;

interface OrderFormPageProps {
  orderId?: string;
  onSaved?: () => void;
}

export function OrderFormPage({ orderId, onSaved }: OrderFormPageProps) {
  const isEdit = !!orderId;
  const { data: order, isLoading: isLoadingOrder } = useOrder(orderId);
  const createMutation = useCreateOrder();
  const updateMutation = useUpdateOrder();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const form = useForm<OrderFormValues>({
    resolver: zodResolver(orderFormSchema),
    defaultValues: {
      tbbn: "",
      customerName: "",
      customerPhone: "",
      addressLine1: "",
      city: "",
      state: "",
      postcode: "",
    },
    values: order
      ? {
          tbbn: order.tbbn,
          customerName: order.customerName ?? "",
          customerPhone: order.customerPhone ?? "",
          addressLine1: order.addressLine1 ?? "",
          city: order.city ?? "",
          state: order.state ?? "",
          postcode: order.postcode ?? "",
        }
      : undefined,
  });

  const handleSubmit = form.handleSubmit(async (values) => {
    setIsSubmitting(true);
    try {
      if (isEdit && orderId) {
        await updateMutation.mutateAsync({
          id: orderId,
          payload: values,
        });
      } else {
        await createMutation.mutateAsync(values);
      }

      if (onSaved) onSaved();
    } finally {
      setIsSubmitting(false);
    }
  });

  const isLoading = isEdit && isLoadingOrder;

  return (
    <div className="max-w-2xl space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight">
          {isEdit ? "Edit Order" : "Create Order"}
        </h1>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">
            Order Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading && (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="mr-2 h-5 w-5 animate-spin" />
              <span>Loading order…</span>
            </div>
          )}

          {!isLoading && (
            <form
              className="space-y-4"
              onSubmit={handleSubmit}
              noValidate
            >
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label className="mb-1 block text-xs font-medium">
                    TBBN *
                  </label>
                  <Input
                    {...form.register("tbbn")}
                    disabled={isEdit}
                    className="text-sm"
                  />
                  {form.formState.errors.tbbn && (
                    <p className="mt-1 text-xs text-red-500">
                      {form.formState.errors.tbbn.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium">
                    Customer Name
                  </label>
                  <Input
                    {...form.register("customerName")}
                    className="text-sm"
                  />
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium">
                    Customer Phone
                  </label>
                  <Input
                    {...form.register("customerPhone")}
                    className="text-sm"
                  />
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium">
                    Address Line 1
                  </label>
                  <Input
                    {...form.register("addressLine1")}
                    className="text-sm"
                  />
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium">
                    City
                  </label>
                  <Input
                    {...form.register("city")}
                    className="text-sm"
                  />
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium">
                    State
                  </label>
                  <Input
                    {...form.register("state")}
                    className="text-sm"
                  />
                </div>

                <div>
                  <label className="mb-1 block text-xs font-medium">
                    Postcode
                  </label>
                  <Input
                    {...form.register("postcode")}
                    className="text-sm"
                  />
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-4">
                <Button
                  type="submit"
                  disabled={isSubmitting}
                >
                  {isSubmitting && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  {isEdit ? "Save Changes" : "Create Order"}
                </Button>
              </div>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
