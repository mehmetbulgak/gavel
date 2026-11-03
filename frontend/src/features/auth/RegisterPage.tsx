import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { Button } from '@/components/ui/Button'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/FormControls'
import { FormError } from '@/components/ui/FormError'
import { describeError } from '@/lib/api/errors'
import { useAuth } from '@/lib/auth/AuthContext'
import { AuthLayout } from './AuthLayout'
import { type RegisterValues, registerSchema } from './schemas'

export default function RegisterPage() {
  const { register: createAccount } = useAuth()
  const [error, setError] = useState<string | null>(null)
  const form = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { displayName: '', email: '', password: '' },
  })
  const { errors, isSubmitting } = form.formState

  const submit = async (values: RegisterValues) => {
    setError(null)
    try {
      await createAccount(values)
    } catch (e) {
      setError(describeError(e))
    }
  }

  return (
    <AuthLayout
      title="Create your account"
      description="Bid on live auctions and list your own items. It takes a minute."
      footer={
        <>
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-cobalt-600 hover:underline">
            Sign in
          </Link>
        </>
      }
    >
      <form className="grid gap-4" onSubmit={form.handleSubmit(submit)} noValidate>
        <FormError message={error} />
        <Field label="Display name" error={errors.displayName?.message} hint="Shown on your listings. Bidders stay anonymous.">
          {(props) => <Input autoComplete="nickname" {...props} {...form.register('displayName')} />}
        </Field>
        <Field label="Email" error={errors.email?.message}>
          {(props) => <Input type="email" autoComplete="email" {...props} {...form.register('email')} />}
        </Field>
        <Field label="Password" error={errors.password?.message} hint="At least 8 characters with a letter and a digit.">
          {(props) => <Input type="password" autoComplete="new-password" {...props} {...form.register('password')} />}
        </Field>
        <Button type="submit" size="lg" loading={isSubmitting}>
          Create account
        </Button>
      </form>
    </AuthLayout>
  )
}
